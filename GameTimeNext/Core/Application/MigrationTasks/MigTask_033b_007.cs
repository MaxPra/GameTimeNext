using GameTimeNext.Core.Framework;
using System.Data.Common;

namespace GameTimeNext.Core.Application.MigrationTasks
{
    internal class MigTask_033b_007
    {

        private static DbConnection? _connection = AppEnvironment.GetDataBaseManager().GetConnection();

        public static void Execute()
        {
            CreateTableT1METAH();
            CreateTableT1METAP();
        }

        private static void CreateTableT1METAH()
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

        private static void CreateTableT1METAP()
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
    }
}
