namespace GameTimeNext.Core.Application.MigrationTasks
{
    internal partial class MigTask_100b_008 : MigTaskBase
    {
        public MigTask_100b_008() : base("1.0.0", requireDb: true)
        {

        }

        protected override void ExecuteImpl()
        {
            MigratePlaythroughTypeValues();
            EnsureExternalConditionGroup();
        }

        private void MigratePlaythroughTypeValues()
        {
            if (_connection == null)
                return;

            using var command = _connection.CreateCommand();
            command.CommandText = @"
                UPDATE T1PLTHR
                SET PTTY = CASE PTTY
                    WHEN 'GTN.DLC' THEN 'D'
                    WHEN 'GTN.NEW_PLAYTHROUGH' THEN 'NP'
                    WHEN 'GTN.INITIAL_PLAYTHROUGH' THEN 'IN'
                    ELSE PTTY
                END
                WHERE PTTY IN ('GTN.DLC', 'GTN.NEW_PLAYTHROUGH', 'GTN.INITIAL_PLAYTHROUGH');";
            command.ExecuteNonQuery();
        }

        private void EnsureExternalConditionGroup()
        {
            if (_connection == null)
                return;

            using var command = _connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO T1GROUP (GRNA, GTYP, CRAT, CHAT)
                VALUES ('External', 'GTN.CONDITION', CURRENT_DATE, CURRENT_TIMESTAMP);";
            command.ExecuteNonQuery();
        }
    }
}
