using GameTimeNext.Core.Application.Metadata;
using GameTimeNext.Core.Application.Metadata.Data;
using GameTimeNext.Core.Framework.Utils;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Framework.DataBase.DevSync
{
    internal static class DevSyncCsvSyncService
    {
        private const char Separator = ';';

        public static void ExportTableFor(UIXTableObjectBase obj)
        {

            if (!FnSystem.IsDebug())
                return;

            if (obj == null || !obj.IsDevSynced)
                return;

            string tableName = obj.GetType().Name;
            ExportTable(tableName);
        }

        public static void ExportTable(string tableName)
        {
            if (!FnSystem.IsDebug())
                return;

            if (string.IsNullOrWhiteSpace(tableName))
                return;

            if (!IsDevSyncEnabledForTable(tableName))
                return;

            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            if (!TableExists(connection, tableName))
                return;

            Dictionary<string, string> columnTypes = GetTableColumnTypes(connection, tableName);
            List<string> columns = columnTypes.Keys.ToList();
            if (columns.Count == 0)
                return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Join(Separator, columns.Select(EscapeCsv)));

            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT * FROM {QuoteIdentifier(tableName)};";

            using SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                List<string> values = new List<string>(columns.Count);
                for (int i = 0; i < columns.Count; i++)
                {
                    object value;

                    if (reader.IsDBNull(i))
                    {
                        value = DBNull.Value;
                    }
                    else if (columnTypes.TryGetValue(columns[i], out string columnType) && IsDateLikeType(columnType))
                    {
                        value = NormalizeDateStringToSqlite(reader.GetString(i));
                    }
                    else
                    {
                        value = reader.GetValue(i);
                    }

                    values.Add(EscapeCsv(ToCsvValue(value)));
                }

                sb.AppendLine(string.Join(Separator, values));
            }

            string devSyncDirectory = GetDevSyncDirectory();
            Directory.CreateDirectory(devSyncDirectory);

            string filePath = Path.Combine(devSyncDirectory, tableName + ".csv");
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

            if (IsCodetableTable(tableName))
                ExportCodetableDefaultFiles();
        }

        private static bool IsCodetableTable(string tableName)
        {
            return string.Equals(tableName, "T1CTABD", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(tableName, "T1CTABH", StringComparison.OrdinalIgnoreCase);
        }

        private static void ExportCodetableDefaultFiles()
        {
            string sourceDirectory = AppEnvironment.GetAppConfig().ImagesSymbolsPathDefault;
            string destinationDirectory = Path.Combine(GetDevSyncDirectory(), "files", "default");

            if (Directory.Exists(destinationDirectory))
                Directory.Delete(destinationDirectory, true);

            if (!Directory.Exists(sourceDirectory))
                return;

            CopyDirectory(sourceDirectory, destinationDirectory);
        }

        private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
        {
            Directory.CreateDirectory(destinationDirectory);

            foreach (string filePath in Directory.GetFiles(sourceDirectory))
            {
                string destinationFilePath = Path.Combine(destinationDirectory, Path.GetFileName(filePath));
                File.Copy(filePath, destinationFilePath, true);
            }

            foreach (string directoryPath in Directory.GetDirectories(sourceDirectory))
            {
                string destinationSubDirectory = Path.Combine(destinationDirectory, Path.GetFileName(directoryPath));
                CopyDirectory(directoryPath, destinationSubDirectory);
            }
        }

        private static bool IsDevSyncEnabledForTable(string tableName)
        {
            if (string.Equals(tableName, "T1METAH", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(tableName, "T1METAP", StringComparison.OrdinalIgnoreCase))
                return true;

            Type? tableType = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        return ex.Types.Where(t => t != null)!;
                    }
                })
                .FirstOrDefault(type =>
                    type != null &&
                    !type.IsAbstract &&
                    typeof(UIXTableObjectBase).IsAssignableFrom(type) &&
                    string.Equals(type.Name, tableName, StringComparison.OrdinalIgnoreCase));

            if (tableType == null)
                return true;

            if (Activator.CreateInstance(tableType) is not UIXTableObjectBase instance)
                return true;

            return instance.IsDevSynced;
        }

        public static void ImportAllFromCsv()
        {

            if (!FnSystem.IsDebug())
                return;

            string devSyncDirectory = GetDevSyncDirectory();
            if (!Directory.Exists(devSyncDirectory))
                return;

            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            string[] files = Directory.GetFiles(devSyncDirectory, "*.csv", SearchOption.TopDirectoryOnly);

            List<string> orderedFiles = files
                .OrderBy(f =>
                {
                    string tableName = ResolveTableNameFromSyncFile(f);
                    if (string.Equals(tableName, "T1METAH", StringComparison.OrdinalIgnoreCase))
                        return 0;
                    if (string.Equals(tableName, "T1METAP", StringComparison.OrdinalIgnoreCase))
                        return 1;
                    return 2;
                })
                .ToList();

            foreach (string file in orderedFiles)
            {
                string tableName = ResolveTableNameFromSyncFile(file);
                if (!string.Equals(tableName, "T1METAH", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(tableName, "T1METAP", StringComparison.OrdinalIgnoreCase))
                    continue;

                ImportTable(file, connection, tableName);
            }

            EnsureMetadataTablesFromImportedMetadata();

            foreach (string file in orderedFiles)
            {
                string tableName = ResolveTableNameFromSyncFile(file);
                if (string.Equals(tableName, "T1METAH", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(tableName, "T1METAP", StringComparison.OrdinalIgnoreCase))
                    continue;

                ImportTable(file, connection, tableName);
            }
        }

        private static void ImportTable(string csvPath, SQLiteConnection connection, string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return;

            string csvText = File.ReadAllText(csvPath, Encoding.UTF8);
            List<List<string>> rows = ParseCsv(csvText);
            if (rows.Count == 0)
                return;

            List<string> headers = rows[0];
            if (headers.Count == 0)
                return;

            EnsureTableSchemaForCsv(connection, tableName, headers, rows);

            Dictionary<string, string> tableColumnTypes = GetTableColumnTypes(connection, tableName);
            List<string> validColumns = headers.Where(h => tableColumnTypes.ContainsKey(h)).ToList();
            if (validColumns.Count == 0)
                return;

            using SQLiteTransaction transaction = connection.BeginTransaction();

            using (SQLiteCommand deleteCmd = connection.CreateCommand())
            {
                deleteCmd.Transaction = transaction;
                deleteCmd.CommandText = $"DELETE FROM {QuoteIdentifier(tableName)};";
                deleteCmd.ExecuteNonQuery();
            }

            string columnSql = string.Join(", ", validColumns.Select(QuoteIdentifier));
            string parameterSql = string.Join(", ", validColumns.Select((_, i) => $"@p{i}"));
            string insertSql = $"INSERT INTO {QuoteIdentifier(tableName)} ({columnSql}) VALUES ({parameterSql});";

            for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                List<string> dataRow = rows[rowIndex];
                if (dataRow.Count == 0)
                    continue;

                using SQLiteCommand insertCmd = connection.CreateCommand();
                insertCmd.Transaction = transaction;
                insertCmd.CommandText = insertSql;

                for (int colIndex = 0; colIndex < validColumns.Count; colIndex++)
                {
                    string columnName = validColumns[colIndex];
                    int headerIndex = headers.IndexOf(columnName);
                    string rawValue = headerIndex >= 0 && headerIndex < dataRow.Count ? dataRow[headerIndex] : string.Empty;

                    object dbValue = ConvertToDbValue(rawValue, tableColumnTypes[columnName]);
                    insertCmd.Parameters.AddWithValue($"@p{colIndex}", dbValue);
                }

                insertCmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        private static string ResolveTableNameFromSyncFile(string filePath)
        {
            string rawName = Path.GetFileNameWithoutExtension(filePath);
            if (string.IsNullOrWhiteSpace(rawName))
                return string.Empty;

            int separatorIndex = rawName.IndexOf('_');
            if (separatorIndex > 0)
            {
                string prefix = rawName.Substring(0, separatorIndex);
                if (prefix.All(char.IsDigit) && separatorIndex + 1 < rawName.Length)
                    return rawName.Substring(separatorIndex + 1);
            }

            return rawName;
        }

        private static void EnsureMetadataTablesFromImportedMetadata()
        {
            TXMETAH txmetah = new TXMETAH();
            TXMETAP txmetap = new TXMETAP();
            CFMetadataTableGenerator generator = new CFMetadataTableGenerator();

            HashSet<string> menamsWithPositions = txmetap
                .ReadAll()
                .Select(x => x.MENAM)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (T1METAH header in txmetah.ReadAll())
            {
                if (!header.GENER)
                    continue;

                if (!menamsWithPositions.Contains(header.MENAM))
                    continue;

                generator.EnsureTableFor(header);
            }
        }

        private static void EnsureTableSchemaForCsv(SQLiteConnection connection, string tableName, List<string> headers, List<List<string>> rows)
        {
            Dictionary<string, string> inferredTypes = InferColumnTypes(headers, rows);

            if (!TableExists(connection, tableName))
            {
                string createColumns = string.Join(", ", headers.Select(h => $"{QuoteIdentifier(h)} {inferredTypes[h]}"));
                string createSql = $"CREATE TABLE IF NOT EXISTS {QuoteIdentifier(tableName)} ({createColumns});";
                using SQLiteCommand createCmd = connection.CreateCommand();
                createCmd.CommandText = createSql;
                createCmd.ExecuteNonQuery();
                return;
            }

            Dictionary<string, string> existingColumns = GetTableColumnTypes(connection, tableName);
            foreach (string header in headers)
            {
                if (existingColumns.ContainsKey(header))
                    continue;

                string alterSql = $"ALTER TABLE {QuoteIdentifier(tableName)} ADD COLUMN {QuoteIdentifier(header)} {inferredTypes[header]};";
                using SQLiteCommand alterCmd = connection.CreateCommand();
                alterCmd.CommandText = alterSql;
                alterCmd.ExecuteNonQuery();
            }
        }

        private static Dictionary<string, string> InferColumnTypes(List<string> headers, List<List<string>> rows)
        {
            Dictionary<string, string> result = headers.ToDictionary(x => x, _ => "TEXT", StringComparer.OrdinalIgnoreCase);

            for (int colIndex = 0; colIndex < headers.Count; colIndex++)
            {
                string header = headers[colIndex];
                string type = "TEXT";

                foreach (List<string> row in rows.Skip(1))
                {
                    if (colIndex >= row.Count)
                        continue;

                    string value = row[colIndex];
                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                    {
                        type = "INTEGER";
                        continue;
                    }

                    if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    {
                        type = "REAL";
                        continue;
                    }

                    if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out _))
                    {
                        type = "TEXT";
                        continue;
                    }

                    type = "TEXT";
                    break;
                }

                result[header] = type;
            }

            return result;
        }

        private static object ConvertToDbValue(string rawValue, string sqliteType)
        {
            if (string.IsNullOrEmpty(rawValue))
                return DBNull.Value;

            string type = sqliteType?.ToUpperInvariant() ?? string.Empty;

            if (IsDateLikeType(type))
            {
                return NormalizeDateStringToSqlite(rawValue);
            }

            if (type.Contains("INT") || type.Contains("BOOL"))
            {
                if (long.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out long longValue))
                    return longValue;
            }

            if (type.Contains("REAL") || type.Contains("FLOA") || type.Contains("DOUB") || type.Contains("DEC"))
            {
                if (double.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleValue))
                    return doubleValue;
            }

            return rawValue;
        }

        private static bool IsDateLikeType(string sqliteType)
        {
            if (string.IsNullOrWhiteSpace(sqliteType))
                return false;

            string type = sqliteType.ToUpperInvariant();
            return type.Contains("DATE") || type.Contains("TIME");
        }

        private static string NormalizeDateStringToSqlite(string rawValue)
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

            if (DateTime.TryParseExact(rawValue, formats, CultureInfo.InvariantCulture, styles, out DateTime exactResult) ||
                DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, styles, out exactResult))
            {
                return exactResult.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }

            return rawValue;
        }

        private static string ToCsvValue(object value)
        {
            if (value == null || value == DBNull.Value)
                return string.Empty;

            if (value is byte[] bytes)
                return Convert.ToBase64String(bytes);

            if (value is IFormattable formattable)
                return formattable.ToString(null, CultureInfo.InvariantCulture);

            return value.ToString() ?? string.Empty;
        }

        private static bool TableExists(SQLiteConnection connection, string tableName)
        {
            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name;";
            cmd.Parameters.AddWithValue("@name", tableName);

            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        private static List<string> GetTableColumns(SQLiteConnection connection, string tableName)
        {
            Dictionary<string, string> columnsWithType = GetTableColumnTypes(connection, tableName);
            return columnsWithType.Keys.ToList();
        }

        private static Dictionary<string, string> GetTableColumnTypes(SQLiteConnection connection, string tableName)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({QuoteIdentifier(tableName)});";

            using SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string name = reader["name"]?.ToString() ?? string.Empty;
                string type = reader["type"]?.ToString() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(name))
                    result[name] = type;
            }

            return result;
        }

        private static string GetDevSyncDirectory()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo? current = new DirectoryInfo(baseDirectory);

            while (current != null)
            {
                string slnxPath = Path.Combine(current.FullName, "GameTimeNext.slnx");
                if (File.Exists(slnxPath))
                    return Path.Combine(current.FullName, "devsync");

                current = current.Parent;
            }

            return Path.Combine(baseDirectory, "devsync");
        }

        private static List<List<string>> ParseCsv(string text)
        {
            List<List<string>> rows = new List<List<string>>();
            List<string> row = new List<string>();
            StringBuilder field = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (!inQuotes && c == Separator)
                {
                    row.Add(field.ToString());
                    field.Clear();
                    continue;
                }

                if (!inQuotes && (c == '\n' || c == '\r'))
                {
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                        i++;

                    row.Add(field.ToString());
                    field.Clear();

                    if (row.Count > 1 || (row.Count == 1 && row[0].Length > 0))
                        rows.Add(row);

                    row = new List<string>();
                    continue;
                }

                field.Append(c);
            }

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                if (row.Count > 1 || (row.Count == 1 && row[0].Length > 0))
                    rows.Add(row);
            }

            return rows;
        }

        private static string EscapeCsv(string value)
        {
            if (value == null)
                return string.Empty;

            bool mustQuote = value.Contains(Separator) || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
            if (!mustQuote)
                return value;

            return '"' + value.Replace("\"", "\"\"") + '"';
        }

        private static string QuoteIdentifier(string identifier)
        {
            return "[" + identifier.Replace("]", "]]", StringComparison.Ordinal) + "]";
        }

        private static void EnsureOpen(SQLiteConnection connection)
        {
            if (connection.State != ConnectionState.Open)
                connection.Open();
        }
    }
}
