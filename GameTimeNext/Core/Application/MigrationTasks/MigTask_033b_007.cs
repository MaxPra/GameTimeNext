namespace GameTimeNext.Core.Application.MigrationTasks
{
    internal class MigTask_033b_007 : MigTaskBase
    {
        // OFDOI: Remove SQL

        public MigTask_033b_007() : base("0.3.3", requireDb: true)
        {

        }

        protected override void ExecuteImpl()
        {
            CreateTableT1METAH();
            CreateTableT1METAP();
            NormalizeLegacyDateFormats();
        }

        private void CreateTableT1METAH()
        {
            var sql = @"
                    CREATE TABLE IF NOT EXISTS T1METAH
                    (
                        MENAM TEXT PRIMARY KEY,
                        DESCR TEXT,
                        MTYPE TEXT,
                        DSYNC INTEGER,
                        GENER INTEGER,
                        CRAT  TEXT,
                        CRUS  TEXT,
                        CHAT  TEXT,
                        CHUS  TEXT
                    );";

            if (_connection == null)
                return;

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        private void CreateTableT1METAP()
        {
            var sql = @"
                    CREATE TABLE IF NOT EXISTS T1METAP
                    (
                        MENAM TEXT NOT NULL,
                        PONAM TEXT NOT NULL,
                        DESCR TEXT,
                        DATYP TEXT,
                        DALEN INTEGER,
                        PORDE INTEGER,
                        PRIMK INTEGER,
                        AUTOI INTEGER,
                        CRAT  TEXT,
                        CRUS  TEXT,
                        CHAT  TEXT,
                        CHUS  TEXT,
                        PRIMARY KEY (MENAM, PONAM)
                    );";

            if (_connection == null)
                return;

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        private void NormalizeLegacyDateFormats()
        {
            NormalizeDateColumns("T1GROUP", "CRAT", "CHAT");
            NormalizeDateColumns("T1METAH", "CRAT", "CHAT");
            NormalizeDateColumns("T1METAP", "CRAT", "CHAT");
            NormalizeDateColumns("T1PLTHR", "CRAT", "CHAT");
            NormalizeDateColumns("T1PROFI", "FIPL", "LAPL", "CRAT", "CHAT");
            NormalizeDateColumns("T1SESSI", "PLFR", "PLTO", "CRAT", "CHAT");
        }

        private void NormalizeDateColumns(string tableName, params string[] columnNames)
        {
            if (_connection == null || columnNames == null || columnNames.Length == 0)
                return;

            string setSql = string.Join(", ", columnNames.Select(columnName =>
                $"{columnName} = CASE " +
                $"WHEN {columnName} LIKE '__.__.____ __:__:__' THEN " +
                $"substr({columnName}, 7, 4) || '-' || substr({columnName}, 4, 2) || '-' || substr({columnName}, 1, 2) || ' ' || substr({columnName}, 12, 5) || ':00' " +
                $"ELSE {columnName} END"));

            string whereSql = string.Join(" OR ", columnNames.Select(columnName =>
                $"{columnName} LIKE '__.__.____ __:__:__'"));

            using var command = _connection.CreateCommand();
            command.CommandText = $"UPDATE {tableName} SET {setSql} WHERE {whereSql};";
            command.ExecuteNonQuery();
        }
    }
}
