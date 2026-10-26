using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.Utils;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Text;
using UIX.ViewController.Engine.Migration.Types;
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
                if ((type.Equals(ImportType.DevSync) || type.Equals(ImportType.MetadataGenerator)) && (!FnSystem.IsDebug() || FnSystem.RunWithUserPermissions())) return;

                // OFDO (#OF001): Continue Logging

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

                        // Metadata
                        filePathMetah = filePaths.Where(p => p.EndsWith("T1METAH.csv")).SingleOrDefault();
                        filePathMetap = filePaths.Where(p => p.EndsWith("T1METAP.csv")).SingleOrDefault();
                        if (!FnString.IsNullEmptyOrWhitespace(filePathMetah))
                            filePaths.Remove(filePathMetah!);
                        if (!FnString.IsNullEmptyOrWhitespace(filePathMetap))
                            filePaths.Remove(filePathMetap!);

                        // OFDO (#OF001): Order files if needed (Codetables head then details)
                        filePathsNotMetadata = filePaths;
                    }

                    // Import metadata tables data first
                    if (!type.Equals(ImportType.MetadataGenerator))
                    {
                        if (filePathMetah is not null)
                        {
                            ImportFromCsv(connection, filePathMetah, migPath);
                            if (filePathMetap is not null)
                                ImportFromCsv(connection, filePathMetap, migPath);
                        }
                    }
                }

                // Apply metadata changes to the database
                MigrateTablesFromMetadata(connection, tableSchemasBeforeImport);

                if (!migPath.Equals(MigrationPath.MetadataGenerator) && !migPath.Equals(MigrationPath.ImportPackages_Create))
                {
                    // Import other tables data
                    foreach (string filePath in filePathsNotMetadata)
                    {
                        ImportFromCsv(connection, filePath, migPath);
                    }

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

            private static void ImportFromCsv(SQLiteConnection connection, string filePath, MigrationPath migPath)
            {
                string tableName = GetTableNameFromFile(filePath);
                List<List<string>> csvLines = GetFileContent(filePath);
                List<string> headers = csvLines[0];

                TableSchema tS = SchemaGenerator.GenerateFromMetadata(tableName: tableName, connection: connection).Single();
                List<ColumnSchema?> headerColumns = headers.Select(h => tS.Columns.Where(c => c.PONAM.Equals(h)).SingleOrDefault()).ToList();

                if (!tableName.Equals("T1CTABD", StringComparison.InvariantCultureIgnoreCase) || migPath.Equals(MigrationPath.DevSync))
                    UIXQuery.ExecuteCustom($"DELETE FROM {tableName};", connection);

                // Do not build INSERT Statement, if nothing needs to be imported
                if (csvLines.Count <= 1) return;

                StringBuilder sb = new StringBuilder();
                sb.Append("INSERT OR IGNORE INTO ").Append(tableName).Append(" (");

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

                    for (int j = 0; j < csvLines[i].Count; j++)
                    {
                        if (j > 0) sb.Append(", ");
                        ColumnSchema? cS = j < headerColumns.Count ? headerColumns[j] : null;
                        sb.Append(ToSqlValue(csvLines[i][j], cS));
                    }

                    if (isLastLine)
                        sb.Append(")");
                    else
                        sb.AppendLine("),");
                }

                sb.Append(";");

                UIXQuery.ExecuteCustom(sb.ToString(), connection);
            }

            private static string ToSqlValue(string valueCsv, ColumnSchema? cS)
            {
                // Unknown column -> Insert as is
                if (cS is null)
                    return $"'{valueCsv.Replace("'", "''")}'";

                object? valueSqlite = UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.CSV, UIXSqliteDataType.ConversionType.Sqlite, valueCsv, cS.DATYP);

                // All columns are NOT NULL -> Fallback to column default
                if (valueSqlite == DBNull.Value)
                    return cS.GetDefaultValue();

                string valueString = Convert.ToString(valueSqlite, CultureInfo.InvariantCulture) ?? string.Empty;
                return $"'{valueString.Replace("'", "''")}'";
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
                try
                {
                    return ParseCsv(File.ReadAllText(filePath, Encoding.UTF8), ToCsv._CSV_SEPERATOR);
                }
                catch (FormatException ex)
                {
                    throw new FormatException($"Invalid CSV file \"{filePath}\": {ex.Message}", ex);
                }
            }

            /// <summary>
            /// Parses CSV content written by ToCsv: quoted fields, doubled quotes and line breaks inside quoted fields.
            /// </summary>
            private static List<List<string>> ParseCsv(string content, char separator)
            {
                List<List<string>> records = new List<List<string>>();
                List<string> record = new List<string>();
                StringBuilder field = new StringBuilder();
                bool inQuotes = false;

                for (int i = 0; i < content.Length; i++)
                {
                    char c = content[i];

                    if (inQuotes)
                    {
                        if (c != '"')
                            field.Append(c);
                        else if (i + 1 < content.Length && content[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                            inQuotes = false;

                        continue;
                    }

                    if (c == '"' && field.Length == 0)
                    {
                        inQuotes = true;
                    }
                    else if (c == separator)
                    {
                        record.Add(field.ToString());
                        field.Clear();
                    }
                    else if (c == '\r' || c == '\n')
                    {
                        if (c == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                            i++;

                        record.Add(field.ToString());
                        field.Clear();
                        records.Add(record);
                        record = new List<string>();
                    }
                    else
                    {
                        field.Append(c);
                    }
                }

                if (inQuotes)
                    throw new FormatException("Unterminated quoted field.");

                // Last record without trailing line break
                if (field.Length > 0 || record.Count > 0)
                {
                    record.Add(field.ToString());
                    records.Add(record);
                }

                return records;
            }

            private static void ImportDefaultFiles(string sourceDirectoryPath)
            {
                string actualSourceDirectoryPath = AppConfig.Dev.GetImagesAndSymbolsDirectoryPath(sourceDirectoryPath);
                string actualTargetDirectoryPath = AppConfig.Storage.DefaultImagesSymbolsDirectoryPath;

                Directory.Delete(actualTargetDirectoryPath, true);
                FnDirectory.EnsureDirectoryExists(actualTargetDirectoryPath);

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
