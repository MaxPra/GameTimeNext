using GameTimeNext.Core.Framework;
using System.Data.SQLite;

namespace GameTimeNext.Core.Application.MigrationTasks
{
    /// <summary>
    /// Enthält die Migrationslogik für Version 0.1.9 Beta
    /// </summary>
    public class MigTask_019b_002
    {
        public static void Execute()
        {
            using SQLiteCommand cmd = AppEnvironment.GetDataBaseManager().GetConnection().CreateCommand();

            cmd.CommandText = "PRAGMA table_info(T1PROFI);";

            bool hasEtml = false;

            using (SQLiteDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string columnName = reader["name"]?.ToString();

                    if (columnName == "ETML")
                    {
                        hasEtml = true;
                        break;
                    }
                }
            }

            if (!hasEtml)
            {
                cmd.CommandText = "ALTER TABLE T1PROFI ADD COLUMN ETML INTEGER;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "UPDATE T1PROFI SET ETML = 0;";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
