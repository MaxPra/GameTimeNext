using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.Utils;
using System.Data.SQLite;
using System.IO;
using System.Text;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Framework.DataBase.Migration
{
    internal static partial class MigrationFactory
    {
        public static class FromCsv
        {
            public static void CreateTables(ImportType type, SQLiteConnection connection)
            {
                MigrateTables(type, connection, MigrationActionType.CREATE);
            }

            public static void MigrateTables(ImportType type)
            {
                MigrateTables(type, null, null);
            }

            private static void MigrateTables(ImportType type, SQLiteConnection? connection, MigrationActionType? overrideActionType)
            {
                if (type.Equals(ImportType.DevSync) && !FnSystem.IsDebug()) return;

                // Determine the source directory path based on the import type
                string sourceDirectoryPath;
                if (type.Equals(ImportType.DevSync))
                    sourceDirectoryPath = AppConfig.Dev.DevSyncDirectoryPath;
                else if (type.Equals(ImportType.ImportPackages))
                {
                    throw new NotImplementedException();
                }
                else
                    throw new NotImplementedException();

                // Get all CSV files paths in the source directory
                List<string> filePaths = Directory.GetFiles(sourceDirectoryPath, "*.csv", SearchOption.TopDirectoryOnly).ToList();
                string? filePathMetah = filePaths.Where(p => p.EndsWith("T1METAH.csv")).SingleOrDefault();
                string? filePathMetap = filePaths.Where(p => p.EndsWith("T1METAP.csv")).SingleOrDefault();
                List<string> filePathsNotMetadata = filePaths.Where(p => !p.EndsWith("T1METAP.csv") && !p.EndsWith("T1METAH.csv")).ToList();

                if (connection is null)
                    connection = AppEnvironment.GetDataBaseManager().GetConnection();

                // Get the table schemas before importing new metadata
                List<TableSchema> tableSchemasBeforeImport = new List<TableSchema>();
                if (overrideActionType is null || !overrideActionType.Equals(MigrationActionType.CREATE))
                    tableSchemasBeforeImport = SchemaGenerator.GenerateFromMetadata();

                // Import matadata tables data first
                if (filePathMetah is not null)
                {
                    ImportFromCsv(connection, filePathMetah);
                    if (filePathMetap is not null)
                        ImportFromCsv(connection, filePathMetap);
                }

                // Apply metadata changes to the database
                MigrateTablesFromMetadata(connection, tableSchemasBeforeImport);

                // Import other tables data
                foreach (string filePath in filePathsNotMetadata)
                    ImportFromCsv(connection, filePath);
            }

            public static void CopyDataToTargetDb(SQLiteConnection oldDb, SQLiteConnection newDb)
            {
                List<TableSchema> oldTableSchemas = SchemaGenerator.GenerateFromActualDatabase(oldDb);
                List<TableSchema> newTableSchemas = SchemaGenerator.GenerateFromActualDatabase(newDb);

                oldTableSchemas.ForEach(oldTableSchema =>
                {
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
        }
    }
}
