using GameTimeNext.Core.Framework.Config;
using System.Data;
using System.Data.SQLite;
using UIX.ViewController.Engine.Migration.Types;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.DataBase
{
    /// <summary>
    /// Connection to the separate log database (written by the LogProvider), independent of the app database of DataBaseManager.
    /// Must not log through FnLog itself: the LogProvider writes into this database, so a log call here would recurse.
    /// </summary>
    class LogDataBaseManager
    {
        private const string T1SYLOG = "T1SYLOG";

        private SQLiteConnection? _connection;

        public LogDataBaseManager() { }

        // [------------------------------------------------]
        // [------------------ PUBLIC ----------------------]
        // [------------------------------------------------]

        /// <summary>
        /// Lock for every access to the connection: entries are logged from several threads (UI, background processes),
        /// and a SQLiteConnection is not thread safe.
        /// </summary>
        public object SyncRoot { get; } = new object();

        public void Initialize()
        {
            Initialize(AppConfig.LogDatabaseFilePath);
        }

        /// <summary>
        /// Initializes the connection to the log database and creates the database if it does not exist yet
        /// </summary>
        public void Initialize(string databaseFilePath)
        {
            lock (SyncRoot)
            {
                ConnectToSQLite(databaseFilePath);
            }
        }

        /// <summary>
        /// Returns the connection to the log database, or null if it is not initialized or could not be opened
        /// </summary>
        public SQLiteConnection? GetConnection()
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
                return null;

            return _connection;
        }

        /// <summary>
        /// Writes the entry into T1SYLOG. Throws if the log database is not open or the insert fails; the caller falls back then.
        /// </summary>
        public void InsertT1sylog(UIXLogEntry entry)
        {
            lock (SyncRoot)
            {
                if (_connection == null || _connection.State != ConnectionState.Open)
                    throw new InvalidOperationException("The log database is not open.");

                using var command = new SQLiteCommand("INSERT INTO T1SYLOG (LGTS, LGTY, LGSR, LGMS, LGEX, LGCL, LGST) VALUES (@LGTS, @LGTY, @LGSR, @LGMS, @LGEX, @LGCL, @LGST);", _connection);

                command.Parameters.AddWithValue("@LGTS", UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.CSharp, UIXSqliteDataType.ConversionType.Sqlite, entry.Timestamp));
                command.Parameters.AddWithValue("@LGTY", entry.LogType.Key);
                command.Parameters.AddWithValue("@LGSR", (object?)entry.Source ?? DBNull.Value);
                command.Parameters.AddWithValue("@LGMS", entry.Message);
                command.Parameters.AddWithValue("@LGEX", (object?)entry.Exception?.ToString() ?? DBNull.Value);
                command.Parameters.AddWithValue("@LGCL", (object?)entry.Caller ?? DBNull.Value);
                command.Parameters.AddWithValue("@LGST", (object?)entry.StackTrace ?? DBNull.Value);

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Reads the newest entries of T1SYLOG (newest first), from inclusive, to exclusive. Empty filter values are ignored;
        /// source and message are compared with LIKE.
        /// Returns an empty list if the log database is not open.
        /// </summary>
        public List<SystemLogRecord> ReadT1sylog(DateTime? from, DateTime? to, string logType, string source, string message, int topX)
        {
            List<SystemLogRecord> records = new List<SystemLogRecord>();

            lock (SyncRoot)
            {
                if (_connection == null || _connection.State != ConnectionState.Open)
                    return records;

                UIXQuery query = new UIXQuery(T1SYLOG, _connection);

                query.AddField(T1SYLOG, "LGID");
                query.AddField(T1SYLOG, "LGTS");
                query.AddField(T1SYLOG, "LGTY");
                query.AddField(T1SYLOG, "LGSR");
                query.AddField(T1SYLOG, "LGMS");
                query.AddField(T1SYLOG, "LGEX");
                query.AddField(T1SYLOG, "LGCL");
                query.AddField(T1SYLOG, "LGST");

                if (from != null)
                    query.AddWhere(T1SYLOG, "LGTS", QueryCompareType.GREATER_OR_EQUAL, UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.CSharp, UIXSqliteDataType.ConversionType.Sqlite, from.Value));

                if (to != null)
                    query.AddWhere(T1SYLOG, "LGTS", QueryCompareType.LESS_THAN, UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.CSharp, UIXSqliteDataType.ConversionType.Sqlite, to.Value));

                if (!FnString.IsNullEmptyOrWhitespace(logType))
                    query.AddWhere(T1SYLOG, "LGTY", QueryCompareType.EQUALS, logType);

                if (!FnString.IsNullEmptyOrWhitespace(source))
                    query.AddWhere(T1SYLOG, "LGSR", QueryCompareType.LIKE, source);

                if (!FnString.IsNullEmptyOrWhitespace(message))
                    query.AddWhere(T1SYLOG, "LGMS", QueryCompareType.LIKE, message);

                query.SetTopX(topX);
                query.AddOrderBy(T1SYLOG, "LGID", OrderDirection.DESC);

                using (var reader = query.Execute())
                {
                    while (reader.Read())
                    {
                        records.Add(new SystemLogRecord
                        {
                            LGID = UIXQuery.GetInt64(reader, T1SYLOG, "LGID"),
                            LGTS = UIXQuery.GetDateTime(reader, T1SYLOG, "LGTS"),
                            LGTY = UIXQuery.GetString(reader, T1SYLOG, "LGTY", string.Empty),
                            LGSR = UIXQuery.GetString(reader, T1SYLOG, "LGSR", string.Empty),
                            LGMS = UIXQuery.GetString(reader, T1SYLOG, "LGMS", string.Empty),
                            LGEX = UIXQuery.GetString(reader, T1SYLOG, "LGEX", string.Empty),
                            LGCL = UIXQuery.GetString(reader, T1SYLOG, "LGCL", string.Empty),
                            LGST = UIXQuery.GetString(reader, T1SYLOG, "LGST", string.Empty)
                        });
                    }
                }
            }

            return records;
        }

        /// <summary>
        /// Deletes all entries of T1SYLOG and shrinks the database file. Throws if the log database is not open.
        /// </summary>
        public void ClearT1sylog()
        {
            lock (SyncRoot)
            {
                if (_connection == null || _connection.State != ConnectionState.Open)
                    throw new InvalidOperationException("The log database is not open.");

                using (var delete = new SQLiteCommand("DELETE FROM T1SYLOG;", _connection))
                    delete.ExecuteNonQuery();

                // Gives the space back to the file system (the log can grow large); not possible inside a transaction
                using (var vacuum = new SQLiteCommand("VACUUM;", _connection))
                    vacuum.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Closes the connection to the log database (on exit, after the last log entry)
        /// </summary>
        public void Close()
        {
            lock (SyncRoot)
            {
                _connection?.Close();
                _connection?.Dispose();
                _connection = null;
            }
        }

        // [------------------------------------------------]
        // [------------------ PRIVATE ---------------------]
        // [------------------------------------------------]

        private void ConnectToSQLite(string databaseFilePath)
        {
            if (_connection != null && _connection.State == ConnectionState.Open)
                return;

            string connectionString = $"Data Source={databaseFilePath};Version=3;New=True;BusyTimeout=5000;Pooling=False;";

            _connection = new SQLiteConnection(connectionString);
            try { _connection.Open(); } catch { return; }

            // Many small inserts: WAL with synchronous NORMAL avoids a full sync per entry; a lost last entry after a power failure is acceptable for logs
            try
            {
                using var pragma = new SQLiteCommand("PRAGMA journal_mode = WAL; PRAGMA synchronous = NORMAL;", _connection);
                pragma.ExecuteNonQuery();
            }
            catch { }

            // Without the table the connection is of no use: close it, so GetConnection returns null and the provider falls back
            try
            {
                EnsureT1sylogExists();
            }
            catch
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
        }

        /// <summary>
        /// Creates the system log table T1SYLOG if it does not exist yet. Plain SQL instead of metadata and table objects:
        /// the log database is separate from the app database and its metadata migration.
        /// </summary>
        private void EnsureT1sylogExists()
        {
            // LGID  ID (ascending, order of the entries)
            // LGTS  timestamp, SQLite format of UIXSqliteDataType.Convert ("yyyy-MM-dd HH:mm:ss", InvariantCulture)
            // LGTY  log type (UIXLogType.Key: Info, Warning, Error, or a type of a subclass)
            // LGSR  source (application or class name), null if none
            // LGMS  message
            // LGEX  exception (ToString), null if none
            // LGCL  place of the call ("Member (File.cs:line)"), null if unknown
            // LGST  call stack at the call, null if not captured
            string sql = @"CREATE TABLE IF NOT EXISTS T1SYLOG (
                               LGID INTEGER PRIMARY KEY AUTOINCREMENT,
                               LGTS TEXT NOT NULL,
                               LGTY TEXT NOT NULL,
                               LGSR TEXT NULL,
                               LGMS TEXT NOT NULL,
                               LGEX TEXT NULL,
                               LGCL TEXT NULL,
                               LGST TEXT NULL
                           );
                           CREATE INDEX IF NOT EXISTS IX_T1SYLOG_LGTS ON T1SYLOG (LGTS);";

            using (var command = new SQLiteCommand(sql, _connection))
                command.ExecuteNonQuery();

            // Log databases created before a column existed get it added (CREATE TABLE IF NOT EXISTS leaves them as they are)
            EnsureT1sylogColumn("LGCL");
            EnsureT1sylogColumn("LGST");
        }

        private void EnsureT1sylogColumn(string column)
        {
            using (var info = new SQLiteCommand("PRAGMA table_info(T1SYLOG);", _connection))
            using (var reader = info.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (string.Equals(reader["name"]?.ToString(), column, StringComparison.OrdinalIgnoreCase))
                        return;
                }
            }

            using var alter = new SQLiteCommand($"ALTER TABLE T1SYLOG ADD COLUMN {column} TEXT NULL;", _connection);
            alter.ExecuteNonQuery();
        }
    }
}
