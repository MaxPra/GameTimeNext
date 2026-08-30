using GameTimeNext.Core.Application.Metadata.Data;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.Utils;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Text;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Framework.DataBase.Migration
{
    internal static partial class MigrationFactory
    {
        private static char _CSV_SEPERATOR = ';';

#if DEBUG
        /// <summary>
        /// Used for development purposes only.
        /// Triggers the export for all tables.
        /// </summary>
        [Obsolete("This method is for development purposes only and cannot be used in production.")]
        public static void DEBUG_ReexportAllTables()
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            List<TableSchema> tableSchemas = GenerateTableSchemasFromActualDatabase(connection);

            foreach (TableSchema tS in tableSchemas)
            {
                TXMETAH txmetah = new TXMETAH();
                T1METAH t1metah = txmetah.Read(tS.MENAM);
                if (!tS.MENAM.Equals("T1METAH") && !tS.MENAM.Equals("T1METAP") && (t1metah is null || !t1metah.DSYNC))
                    continue;

                ExportCsvFile(tS.MENAM);
            }
        }
#endif

        public static void ExportCsvFileFor(UIXTableObjectBase obj)
        {
            if (!FnSystem.IsDebug()) return;

            if (obj is null || !obj.IsDevSynced) return;

            string tableName = obj.GetType().Name;
            ExportCsvFile(tableName);
        }

        public static void ExportCsvFile(string tableName)
        {
            if (!FnSystem.IsDebug()) return;

            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();

            TableSchema? tableSchema = GenerateTableSchemasFromActualDatabase(connection).Where(s => s.MENAM.Equals(tableName)).SingleOrDefault();
            if (tableSchema is null) return;

            Dictionary<string, SqliteDataType> columns = tableSchema.Columns.ToDictionary(
                c => c.PONAM,
                c => c.GetSqliteDataType()
            );
            List<string> csvLines = new List<string>()
            {
                String.Join(_CSV_SEPERATOR, columns.Keys)
            };

            using (var reader = UIXQuery.QueryCustom($"SELECT {tableSchema.GetColumnNamesForSql()} FROM {tableName};", connection))
            {
                while (reader.Read())
                {
                    List<string> values = new List<string>();
                    for (int i = 0; i < columns.Count; i++)
                    {
                        SqliteDataType type = columns.ElementAt(i).Value;
                        object value;

                        if (reader.IsDBNull(i))
                            value = DBNull.Value;
                        if (type.Key.Equals("05"))
                            value = DateToCsvValue(reader.GetString(i));
                        else
                            value = reader.GetValue(i);

                        values.Add(EscapeCsv(ToCsvValue(value)));
                    }

                    csvLines.Add(String.Join(_CSV_SEPERATOR, values));
                }

                reader.Close();
            }

            string fileContent = String.Join(Environment.NewLine, csvLines);

            if (!Directory.Exists(AppConfig.Dev.DevSyncDirectoryPath))
                Directory.CreateDirectory(AppConfig.Dev.DevSyncDirectoryPath);

            string filePath = Path.Combine(AppConfig.Dev.DevSyncDirectoryPath, $"{tableName}.csv");
            File.WriteAllText(filePath, fileContent, Encoding.UTF8);

            if (IsCodetableTable(tableName))
                ExportCodetableDefaultFiles();
        }

        private static string EscapeCsv(string value)
        {
            if (value is null)
                return string.Empty;

            bool mustQuote = value.Contains(_CSV_SEPERATOR) || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
            if (!mustQuote)
                return value;

            return '"' + value.Replace("\"", "\"\"") + '"';
        }

        private static string ToCsvValue(object value)
        {
            if (value is null || value == DBNull.Value)
                return string.Empty;

            if (value is bool)
                return (bool)value ? "1" : "0";

            if (value is IFormattable formattable)
                return formattable.ToString(null, CultureInfo.InvariantCulture);

            return value.ToString() ?? string.Empty;
        }

        private static string DateToCsvValue(string value)
        {
            DateTimeStyles styles = DateTimeStyles.AllowWhiteSpaces;
            string[] formats =
            {
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd HH:mm:ss.FFFFFFF",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
                "dd.MM.yyyy HH:mm:ss",
                "dd.MM.yyyy HH:mm",
                "o"
            };

            if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, styles, out DateTime exactResult) || DateTime.TryParse(value, CultureInfo.InvariantCulture, styles, out exactResult))
                return exactResult.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            return value;
        }

        private static bool IsCodetableTable(string tableName)
        {
            return string.Equals(tableName, "T1CTABD", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(tableName, "T1CTABH", StringComparison.OrdinalIgnoreCase);
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
    }
}
