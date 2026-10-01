using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.Utils;
using System.Data.SQLite;
using System.IO;
using System.Text;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.DataBase.Migration
{
    internal static partial class MigrationFactory
    {
        public static class FromCsv
        {
            public static void CreateTables(ImportType type, SQLiteConnection connection)
            {
                LogInfo($"Running (ImportType: \"{type.ToString()}\")...", subSystem: "FromCsv", method: "CreateTables");
                MigrateTables(type, connection, MigrationActionType.CREATE);
            }

            public static void MigrateTables(ImportType type, string? importDirectoryPathOverride = null)
            {
                LogInfo($"Running (ImportType: \"{type.ToString()}\")...", subSystem: "FromCsv", method: "MigrateTables");
                MigrateTables(type, null, null, importDirectoryPathOverride: importDirectoryPathOverride);
            }

            private static void MigrateTables(ImportType type, SQLiteConnection? connection, MigrationActionType? overrideActionType, string? importDirectoryPathOverride = null)
            {
                if ((type.Equals(ImportType.DevSync) || type.Equals(ImportType.MetadataGenerator)) && !FnSystem.IsDebug()) return;

                // OFDO: Continue Logging

                MigrationPath migPath = GetMigrationPath(type, overrideActionType);

                // Determine the source directory path based on the import type
                string sourceDirectoryPath;
                if (migPath.Equals(MigrationPath.DevSync))
                {
                    sourceDirectoryPath = AppConfig.Dev.DevSyncDirectoryPath;
                }
                else if (migPath.Equals(MigrationPath.ImportPackages))
                {
                    if (FnString.IsNullEmptyOrWhitespace(importDirectoryPathOverride))
                        throw new ArgumentException("Input directory path cannot be null or empty for ImportPackages export type.");

                    sourceDirectoryPath = importDirectoryPathOverride!;
                }
                else if (migPath.Equals(MigrationPath.MetadataGenerator) || migPath.Equals(MigrationPath.ImportPackages_Create))
                {
                    sourceDirectoryPath = string.Empty; // Not needed
                }
                else
                {
                    throw new NotImplementedException();
                }

                if (connection is null)
                    connection = AppEnvironment.GetDataBaseManager().GetConnection();

                // Get the table schemas before importing new metadata
                List<TableSchema> tableSchemasBeforeImport = new List<TableSchema>();
                if (overrideActionType is null || !overrideActionType.Equals(MigrationActionType.CREATE))
                {
                    if (type.Equals(ImportType.MetadataGenerator))
                        tableSchemasBeforeImport = SchemaGenerator.GenerateFromActualDatabase(connection);
                    else
                        tableSchemasBeforeImport = SchemaGenerator.GenerateFromMetadata();
                }

                List<string> filePathsNotMetadata = new List<string>();
                if (!migPath.Equals(MigrationPath.ImportPackages_Create))
                {
                    // Get all CSV files paths in the source directory
                    List<string> filePaths = new List<string>();
                    string? filePathMetah = null;
                    string? filePathMetap = null;
                    if (!type.Equals(ImportType.MetadataGenerator))
                    {
                        filePaths = Directory.GetFiles(sourceDirectoryPath, "*.csv", SearchOption.TopDirectoryOnly).ToList();
                        filePathMetah = filePaths.Where(p => p.EndsWith("T1METAH.csv")).SingleOrDefault();
                        filePathMetap = filePaths.Where(p => p.EndsWith("T1METAP.csv")).SingleOrDefault();
                        filePathsNotMetadata = filePaths.Where(p => !p.EndsWith("T1METAP.csv") && !p.EndsWith("T1METAH.csv")).ToList();
                    }

                    // Import matadata tables data first
                    if (!type.Equals(ImportType.MetadataGenerator))
                    {
                        if (filePathMetah is not null)
                        {
                            ImportFromCsv(connection, filePathMetah);
                            if (filePathMetap is not null)
                                ImportFromCsv(connection, filePathMetap);
                        }
                    }
                }

                // Apply metadata changes to the database
                MigrateTablesFromMetadata(connection, tableSchemasBeforeImport);

                if (!migPath.Equals(MigrationPath.MetadataGenerator) && !migPath.Equals(MigrationPath.ImportPackages_Create))
                {
                    // Import other tables data
                    foreach (string filePath in filePathsNotMetadata)
                    ImportFromCsv(connection, filePath);

                    // Import imagesAndSymbols
                    ImportDefaultFiles(sourceDirectoryPath);
                }
            }

            public static void CopyDataToTargetDb(SQLiteConnection oldDb, SQLiteConnection newDb, bool metadata = false)
            {
                List<TableSchema> oldTableSchemas = SchemaGenerator.GenerateFromActualDatabase(oldDb);
                List<TableSchema> newTableSchemas;
                if (metadata)
                    newTableSchemas = MigrationFactory.Metadata.METADATA;
                else
                    newTableSchemas = SchemaGenerator.GenerateFromMetadata(connection: newDb);

                oldTableSchemas.ForEach(oldTableSchema =>
                {
                    if (!oldTableSchema.IsMetadataTable.Equals(metadata)) return;

                    TableSchema? newTableSchema = newTableSchemas.Where(s => s.MENAM.Equals(oldTableSchema.MENAM)).SingleOrDefault();
                    if (newTableSchema is null) return;

                    List<string> sqlLines = new List<string>()
                    {
                        $"ATTACH DATABASE '{oldDb.FileName}' AS olddb;",
                        $"REPLACE INTO main.{oldTableSchema.MENAM} ({oldTableSchema.GetColumnNamesForSql(newTableSchema)})",
                        $"SELECT {oldTableSchema.GetColumnNamesForSql(newTableSchema)}",
                        $"FROM olddb.{oldTableSchema.MENAM};",
                        $"DETACH DATABASE olddb;",
                    };

                    UIXQuery.ExecuteCustom(String.Join(Environment.NewLine, sqlLines), newDb);
                });
            }

            private static void ImportFromCsv(SQLiteConnection connection, string filePath)
            {
                string tableName = GetTableNameFromFile(filePath);
                List<List<string>> csvLines = GetFileContent(filePath);
                List<string> headers = csvLines[0];

                UIXQuery.ExecuteCustom($"DELETE FROM {tableName};", connection);

                StringBuilder sb = new StringBuilder();
                sb.Append($"INSERT INTO {tableName} (");

                bool firstHeaderAdded = false;
                foreach (string header in headers)
                {
                    if (firstHeaderAdded) sb.Append(", ");
                    sb.Append(header);
                    firstHeaderAdded = true;
                }
                sb.AppendLine(")");
                sb.AppendLine("VALUES");

                for (int i = 1; i < csvLines.Count; i++)
                {
                    bool isLastLine = i.Equals(csvLines.Count - 1);
                    sb.Append("(");

                    bool firstValueAdded = false;
                    foreach (string value in csvLines[i])
                    {
                        if (firstValueAdded) sb.Append(", ");
                        sb.Append($"'{value}'");
                        firstValueAdded = true;
                    }

                    if (isLastLine)
                        sb.Append(")");
                    else
                        sb.AppendLine("),");
                }

                sb.Append(";");

                // OFDOI: Value-Conversion from CSV format to SQLite format

                UIXQuery.ExecuteCustom(sb.ToString(), connection);
            }

            private static void MigrateTablesFromMetadata(SQLiteConnection connection, List<TableSchema> tableSchemasBeforeImport)
            {
                List<MigrationAction> actions = MigrationActionGenerator.Generate(connection, tableSchemasBeforeImport);
                actions.ForEach(a => a.Migrate(connection));
            }

            private static string GetTableNameFromFile(string filePath)
            {
                FileInfo fI = new FileInfo(filePath);
                return fI.Name.Split('.').First();
            }

            private static List<List<string>> GetFileContent(string filePath)
            {
                FileInfo fileInfo = new FileInfo(filePath);
                return File.ReadAllLines(filePath, Encoding.UTF8).Select(l => l.Split(ToCsv._CSV_SEPERATOR).ToList()).ToList();
            }

            private static void ImportDefaultFiles(string sourceDirectoryPath)
            {
                string actualSourceDirectoryPath = AppConfig.Dev.GetImagesAndSymbolsDirectoryPath(sourceDirectoryPath);
                string actualTargetDirectoryPath = AppConfig.Storage.DefaultImagesSymbolsDirectoryPath;

                CopyDirectory(actualSourceDirectoryPath, actualTargetDirectoryPath);
            }

            private static MigrationPath GetMigrationPath(ImportType importType, MigrationActionType? overrideActionType)
            {
                MigrationPath temp;

                switch (importType)
                {
                    case ImportType.DevSync:
                        temp = MigrationPath.DevSync;
                        break;
                    case ImportType.ImportPackages:
                        temp = MigrationPath.ImportPackages;
                        break;
                    case ImportType.MetadataGenerator:
                        temp = MigrationPath.MetadataGenerator;
                        break;
                    default:
                        throw new NotImplementedException();
                }

                if (temp.Equals(MigrationPath.ImportPackages))
                {
                    if (MigrationActionType.CREATE.Equals(overrideActionType))
                    {
                        temp = MigrationPath.ImportPackages_Create;
                    }
                }

                return temp;
            }

            private enum MigrationPath
            {
                // Standalone-Paths
                DevSync,
                ImportPackages,
                MetadataGenerator,

                // Multilayer-Paths
                ImportPackages_Create,
            }
        }
    }
}
