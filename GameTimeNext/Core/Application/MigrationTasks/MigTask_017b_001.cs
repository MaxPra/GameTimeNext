using GameTimeNext.Core.Framework;
using System.Data.SQLite;

namespace GameTimeNext.Core.Application.MigrationTasks
{
    /// <summary>
    /// Migriert die neuen Felder im T1PROFI hinzu (ETMA, ETME, ETCO)
    /// </summary>
    public class MigTask_017b_001
    {
        public static void Execute()
        {
            using SQLiteCommand cmd = AppEnvironment.GetDataBaseManager().GetConnection().CreateCommand();

            cmd.CommandText = "ALTER TABLE T1PROFI ADD COLUMN ETMA REAL;";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "ALTER TABLE T1PROFI ADD COLUMN ETME REAL;";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "ALTER TABLE T1PROFI ADD COLUMN ETCO REAL;";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "ALTER TABLE T1PROFI ADD COLUMN ETTY VARCHAR(200);";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "UPDATE T1PROFI SET ETTY = 'GTN.EST_TIME.NONE';";
            cmd.ExecuteNonQuery();
        }
    }
}
