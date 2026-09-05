using UIX.ViewController.Engine.Querying;

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
            EnsureT1plthrPtpaIsFalse();
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

        private void EnsureT1plthrPtpaIsFalse()
        {
            if (_connection is null) return;

            string sql = $"UPDATE T1PLTHR SET PTPA = 0 WHERE PTPA IS NULL;";
            UIXQuery.ExecuteCustom(sql, _connection);
        }
    }
}
