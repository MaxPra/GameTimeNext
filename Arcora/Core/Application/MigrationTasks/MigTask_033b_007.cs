namespace GameTimeNext.Core.Application.MigrationTasks
{
    internal class MigTask_033b_007 : MigTaskBase
    {
        public MigTask_033b_007() : base("0.3.3", requireDb: true)
        {

        }

        protected override void ExecuteImpl()
        {
            NormalizeLegacyDateFormats();
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
