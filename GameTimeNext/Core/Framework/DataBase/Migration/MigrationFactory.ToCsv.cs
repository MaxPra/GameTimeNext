using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.Utils;
using System.Data.SQLite;
using System.IO;
using System.Text;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Framework.DataBase.Migration
{
    internal static partial class MigrationFactory
    {
        public static class ToCsv
        {
            public static char _CSV_SEPERATOR = ';';

            public static void ExportCsvFileFor(SQLiteConnection connection, UIXTableObjectBase obj)
            {
                if (!FnSystem.IsDebug()) return;
                if (obj is null || !obj.IsDevSynced) return;

                string tableName = obj.GetType().Name;
                if (tableName.StartsWith("T1META")) return;

                ExportCsvFileFor(connection, tableName);
            }

            public static void ExportCsvFileFor(SQLiteConnection connection, string tableName)
            {
                if (!FnSystem.IsDebug()) return;

                TableSchema? tS = SchemaGenerator.GenerateSingleFromMetadata(tableName);
                if (tS is null) return;

                // Get column names and types
                Dictionary<string, SqliteDataType> columns = tS.Columns.ToDictionary(
                    c => c.PONAM,
                    c => c.DATYP
                );

                // Add headers
                List<string> csvLines = new List<string>()
            {
                String.Join(_CSV_SEPERATOR, columns.Keys),
            };

                using (var reader = UIXQuery.QueryCustom($"SELECT {tS.GetColumnNamesForSql()} FROM {tableName};", connection))
                {
                    // Iterate rows
                    while (reader.Read())
                    {
                        List<string> values = new List<string>();
                        // Iterate columns
                        for (int i = 0; i < columns.Count; i++)
                        {
                            SqliteDataType dataType = columns.ElementAt(i).Value;
                            object value;

                            if (reader.IsDBNull(i))
                                value = DBNull.Value;
                            else if (dataType.Key.Equals("05"))
                                value = SqliteCsvDataConverter.DateToCsv(reader.GetString(i));
                            else
                                value = reader.GetValue(i);

                            values.Add(SqliteCsvDataConverter.EscapeCsv(SqliteCsvDataConverter.ToCsvValue(value), _CSV_SEPERATOR));
                        }

                        csvLines.Add(String.Join(_CSV_SEPERATOR, values));
                    }

                    reader.Close();
                }

                if (!Directory.Exists(AppConfig.Dev.DevSyncDirectoryPath))
                    Directory.CreateDirectory(AppConfig.Dev.DevSyncDirectoryPath);

                string filePath = Path.Combine(AppConfig.Dev.DevSyncDirectoryPath, $"{tableName}.csv");
                File.WriteAllLines(filePath, csvLines, Encoding.UTF8);

                if (tS.IsCodetableTable)
                    ExportCodetableDefaultFiles();
            }

            private static void ExportCodetableDefaultFiles()
            {
                string sourceDirectoryPath = AppConfig.Storage.DefaultImagesSymbolsDirectoryPath;
                string destinationDirectoryPath = AppConfig.Dev.DevSyncDefaultImagesSymbolsDirectoryPath;

                if (Directory.Exists(destinationDirectoryPath))
                    Directory.Delete(destinationDirectoryPath, true);

                if (!Directory.Exists(sourceDirectoryPath))
                    return;

                CopyDirectory(sourceDirectoryPath, destinationDirectoryPath);
            }

            private static void CopyDirectory(string sourceDirectoryPath, string destinationDirectoryPath)
            {
                if (!Directory.Exists(sourceDirectoryPath)) return;

                List<string> filePaths = Directory.GetFiles(sourceDirectoryPath, "*", SearchOption.TopDirectoryOnly).ToList();

                Parallel.ForEach(filePaths, filePath =>
                {
                    FileInfo fileInfo = new FileInfo(filePath);
                    string newPath = Path.Combine(destinationDirectoryPath, fileInfo.Name);
                    File.Copy(fileInfo.FullName, newPath, true);
                });
            }

#if DEBUG
            /// <summary>
            /// Used for development purposes only.
            /// Triggers the export for all tables.
            /// </summary>
            [Obsolete("This method is for development purposes only and cannot be used in production.")]
            public static void DEBUG_ReexportAllTables()
            {
                SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
                using (var reader = UIXQuery.QueryCustom($"SELECT MENAM, DSYNC FROM T1METAH;", connection))
                {
                    while (reader.Read())
                    {
                        string menam = UIXQuery.GetString(reader, "MENAM");
                        bool dsync = UIXQuery.GetBool(reader, "DSYNC");
                        // Only DSYNC enabled tables and metadata tables are exported
                        if (!menam.Equals("T1METAH") && !menam.Equals("T1METAP") && !dsync)
                            continue;

                        ExportCsvFileFor(connection, menam);
                    }

                    reader.Close();
                }
            }
#endif
        }
    }
}
