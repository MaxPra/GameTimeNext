using GameTimeNext.Core.Application.Metadata.Data;
using GameTimeNext.Core.Framework;
using System.Data.SQLite;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Metadata
{
    public sealed class CFMetadataTableGenerator
    {
        public sealed class TableGenerationResult
        {
            public bool TableExisted { get; set; }
            public string TableName { get; set; } = string.Empty;
            public List<string> ExecutedSql { get; } = new List<string>();
            public List<string> RemovedColumns { get; } = new List<string>();
        }

        public TableGenerationResult EnsureTableFor(T1METAH t1metah)
        {
            if (t1metah == null)
                throw new ArgumentNullException(nameof(t1metah));

            string tableName = NormalizeIdentifier(t1metah.MENAM);
            if (string.IsNullOrWhiteSpace(tableName))
                throw new InvalidOperationException("MENAM is required for table generation.");

            List<T1METAP> positions = new TXMETAP()
                .ReadAll()
                .Where(x => string.Equals(x.MENAM, t1metah.MENAM, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.PORDE)
                .ThenBy(x => x.PONAM)
                .ToList();

            if (positions.Count == 0)
                throw new InvalidOperationException($"No metadata positions found for '{t1metah.MENAM}'.");

            List<TableColumnDefinition> columns = BuildColumns(positions);

            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            TableGenerationResult result = new TableGenerationResult
            {
                TableExisted = TableExists(connection, tableName),
                TableName = tableName
            };

            if (!result.TableExisted)
            {
                string createSql = BuildCreateTableSql(tableName, columns);
                ExecuteNonQuery(connection, createSql);
                result.ExecutedSql.Add(createSql);
                return result;
            }

            Dictionary<string, string> existingColumns = GetExistingColumns(connection, tableName);
            HashSet<string> targetColumnNames = columns
                .Select(x => x.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            List<string> columnsToRemove = existingColumns
                .Keys
                .Where(x => !targetColumnNames.Contains(x))
                .ToList();

            if (columnsToRemove.Count > 0)
            {
                RebuildTable(connection, tableName, columns, existingColumns, result);
                result.RemovedColumns.AddRange(columnsToRemove);
                return result;
            }

            foreach (TableColumnDefinition column in columns)
            {
                if (existingColumns.ContainsKey(column.Name))
                    continue;

                string alterSql = $"ALTER TABLE {QuoteIdentifier(tableName)} ADD COLUMN {BuildColumnSql(column, includePrimaryKey: false)};";
                ExecuteNonQuery(connection, alterSql);
                result.ExecutedSql.Add(alterSql);
            }

            return result;
        }

        public bool DeleteTableFor(T1METAH t1metah)
        {
            if (t1metah == null)
                throw new ArgumentNullException(nameof(t1metah));

            return DeleteTable(t1metah.MENAM);
        }

        public bool DeleteTable(string menam)
        {
            string tableName = NormalizeIdentifier(menam);
            if (string.IsNullOrWhiteSpace(tableName))
                return false;

            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            if (!TableExists(connection, tableName))
                return false;

            string dropSql = $"DROP TABLE IF EXISTS {QuoteIdentifier(tableName)};";
            ExecuteNonQuery(connection, dropSql);

            return true;
        }

        private static void RebuildTable(
            SQLiteConnection connection,
            string tableName,
            List<TableColumnDefinition> targetColumns,
            Dictionary<string, string> existingColumns,
            TableGenerationResult result)
        {
            string tempTableName = tableName + "__OLD_" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();

            using SQLiteTransaction transaction = connection.BeginTransaction();

            try
            {
                string renameSql = $"ALTER TABLE {QuoteIdentifier(tableName)} RENAME TO {QuoteIdentifier(tempTableName)};";
                ExecuteNonQuery(connection, renameSql, transaction);
                result.ExecutedSql.Add(renameSql);

                string createSql = BuildCreateTableSql(tableName, targetColumns);
                ExecuteNonQuery(connection, createSql, transaction);
                result.ExecutedSql.Add(createSql);

                List<string> sharedColumns = targetColumns
                    .Select(x => x.Name)
                    .Where(x => existingColumns.ContainsKey(x))
                    .ToList();

                if (sharedColumns.Count > 0)
                {
                    string targetColumnSql = string.Join(", ", sharedColumns.Select(QuoteIdentifier));
                    string sourceColumnSql = string.Join(", ", sharedColumns.Select(QuoteIdentifier));
                    string copySql =
                        $"INSERT INTO {QuoteIdentifier(tableName)} ({targetColumnSql}) " +
                        $"SELECT {sourceColumnSql} FROM {QuoteIdentifier(tempTableName)};";

                    ExecuteNonQuery(connection, copySql, transaction);
                    result.ExecutedSql.Add(copySql);
                }

                string dropSql = $"DROP TABLE {QuoteIdentifier(tempTableName)};";
                ExecuteNonQuery(connection, dropSql, transaction);
                result.ExecutedSql.Add(dropSql);

                transaction.Commit();
            }
            catch
            {
                try
                {
                    transaction.Rollback();
                }
                catch
                {
                }

                throw;
            }
        }

        private static List<TableColumnDefinition> BuildColumns(List<T1METAP> positions)
        {
            List<TableColumnDefinition> columns = new List<TableColumnDefinition>();
            foreach (T1METAP position in positions)
            {
                string sqliteType = NormalizeSqliteType(UIXSQLiteDataTypes.FromCSharp(position.DATYP));

                if (sqliteType == "TEXT" && position.DALEN > 0)
                    sqliteType = $"VARCHAR({position.DALEN})";

                if (position.AUTOI)
                    sqliteType = "INTEGER";

                columns.Add(new TableColumnDefinition
                {
                    Name = NormalizeIdentifier(position.PONAM),
                    SqliteType = sqliteType,
                    IsPrimaryKey = position.PRIMK,
                    IsAutoIncrement = position.AUTOI
                });
            }

            return columns;
        }

        private static string BuildCreateTableSql(string tableName, List<TableColumnDefinition> columns)
        {
            int autoIncrementIndex = columns.FindIndex(x => x.IsAutoIncrement);
            if (autoIncrementIndex >= 0 && columns.Count(x => x.IsAutoIncrement) > 1)
                throw new InvalidOperationException("Only one AUTOINCREMENT column is allowed.");

            List<string> columnDefinitions = columns
                .Select(column => BuildColumnSql(column, includePrimaryKey: false))
                .ToList();

            if (autoIncrementIndex >= 0)
            {
                bool hasAdditionalPrimaryKeys = columns
                    .Where((x, index) => index != autoIncrementIndex)
                    .Any(x => x.IsPrimaryKey);

                if (hasAdditionalPrimaryKeys)
                    throw new InvalidOperationException("AUTOINCREMENT cannot be combined with additional primary keys.");

                columnDefinitions[autoIncrementIndex] += " PRIMARY KEY AUTOINCREMENT";
                return $"CREATE TABLE IF NOT EXISTS {QuoteIdentifier(tableName)} ({string.Join(", ", columnDefinitions)});";
            }

            List<string> primaryKeys = columns
                .Where(x => x.IsPrimaryKey)
                .Select(x => QuoteIdentifier(x.Name))
                .ToList();

            if (primaryKeys.Count == 1)
            {
                string primaryKeyColumn = primaryKeys[0];
                int index = columns.FindIndex(x => QuoteIdentifier(x.Name) == primaryKeyColumn);
                columnDefinitions[index] += " PRIMARY KEY";
            }
            else if (primaryKeys.Count > 1)
            {
                columnDefinitions.Add($"PRIMARY KEY ({string.Join(", ", primaryKeys)})");
            }

            return $"CREATE TABLE IF NOT EXISTS {QuoteIdentifier(tableName)} ({string.Join(", ", columnDefinitions)});";
        }

        private static string BuildColumnSql(TableColumnDefinition column, bool includePrimaryKey)
        {
            string primaryKeySuffix = includePrimaryKey && column.IsPrimaryKey ? " PRIMARY KEY" : string.Empty;
            return $"{QuoteIdentifier(column.Name)} {column.SqliteType}{primaryKeySuffix}";
        }

        private static Dictionary<string, string> GetExistingColumns(SQLiteConnection connection, string tableName)
        {
            Dictionary<string, string> columns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({QuoteIdentifier(tableName)});";

            using SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string name = reader["name"]?.ToString() ?? string.Empty;
                string type = reader["type"]?.ToString() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(name))
                    columns[name] = type;
            }

            return columns;
        }

        private static string NormalizeSqliteType(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "TEXT";

            return input.Trim().ToUpperInvariant();
        }

        private static bool TableExists(SQLiteConnection connection, string tableName)
        {
            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @name;";
            cmd.Parameters.AddWithValue("@name", tableName);

            return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        private static void EnsureOpen(SQLiteConnection connection)
        {
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();
        }

        private static void ExecuteNonQuery(SQLiteConnection connection, string sql, SQLiteTransaction? transaction = null)
        {
            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            if (transaction != null)
                cmd.Transaction = transaction;

            cmd.ExecuteNonQuery();
        }

        private static string NormalizeIdentifier(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            string cleaned = new string(input.Trim().Where(ch => char.IsLetterOrDigit(ch) || ch == '_').ToArray());
            if (string.IsNullOrWhiteSpace(cleaned))
                return string.Empty;

            if (char.IsDigit(cleaned[0]))
                cleaned = "_" + cleaned;

            return cleaned.ToUpperInvariant();
        }

        private static string QuoteIdentifier(string identifier)
        {
            return $"\"{identifier.Replace("\"", "\"\"")}\"";
        }

        private sealed class TableColumnDefinition
        {
            public string Name { get; set; } = string.Empty;
            public string SqliteType { get; set; } = "TEXT";
            public bool IsPrimaryKey { get; set; }
            public bool IsAutoIncrement { get; set; }
        }
    }
}
