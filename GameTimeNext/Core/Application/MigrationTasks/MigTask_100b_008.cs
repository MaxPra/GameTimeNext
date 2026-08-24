using GameTimeNext.Core.Framework;
using System.Data.Common;

namespace GameTimeNext.Core.Application.MigrationTasks
{
    internal partial class MigTask_100b_008
    {
        private static readonly DbConnection? _connection = AppEnvironment.GetDataBaseManager().GetConnection();

        public static void Execute()
        {
            EnsureOpen();
            MigratePlaythroughTypeValues();
            EnsureExternalConditionGroup();
        }

        private static void EnsureOpen()
        {
            if (_connection != null && _connection.State != System.Data.ConnectionState.Open)
                _connection.Open();
        }

        private static void MigratePlaythroughTypeValues()
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

        private static void EnsureExternalConditionGroup()
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
