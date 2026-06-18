using GameTimeNext.Core.Framework;
using System.Data.Common;

namespace GameTimeNext.Core.Application.MigrationTasks
{
    internal class MigTask_032b_006
    {

        private static DbConnection? _connection = AppEnvironment.GetDataBaseManager().GetConnection();


        internal static void Execute()
        {
            CreateTableT1CTABH();
            CreateTableT1CTABD();
        }

        private static void CreateTableT1CTABH()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS T1CTABH (
                            TXTYP VARCHAR(200) PRIMARY KEY,
                            DESCR VARCHAR(200),
                            PERMI VARCHAR(200),

                            PAAC1 INTEGER,
                            PADE1 VARCHAR(200),
                            PARF1 INTEGER,
                            PACO1 VARCHAR(200),
                            PACT1 VARCHAR(200),

                            PAAC2 INTEGER,
                            PADE2 VARCHAR(200),
                            PARF2 INTEGER,
                            PACO2 VARCHAR(200),
                            PACT2 VARCHAR(200),

                            CRAT DATETIME,
                            CHAT DATETIME
                        );";

            if (_connection == null)
                return;

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        private static void CreateTableT1CTABD()
        {
            var sql = @"
                    CREATE TABLE IF NOT EXISTS T1CTABD
                    (
                        TXTYP TEXT NOT NULL,
                        TXNUM TEXT NOT NULL,
                        DESCR TEXT,
                        CRAT  TEXT,
                        CHAT  TEXT,
                        PARM1 TEXT,
                        PARM2 TEXT,
                        PRIMARY KEY (TXTYP, TXNUM)
                    );";

            if (_connection == null)
                return;

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

    }
}
