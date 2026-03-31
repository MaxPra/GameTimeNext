using GameTimeNext.Core.Framework;

namespace GameTimeNext.Core.Application.MigrationTasks
{
    public class MigTask_020b_003
    {

        public static void Execute()
        {
            var connection = AppEnvironment.GetDataBaseManager().GetConnection();

            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            using (var checkCmd = connection.CreateCommand())
            {
                checkCmd.CommandText = "PRAGMA table_info(T1PLTHR);";

                using (var reader = checkCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string columnName = reader["name"]?.ToString();

                        if (string.Equals(columnName, "PTCA", StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }
                    }
                }
            }

            using (var alterCmd = connection.CreateCommand())
            {
                alterCmd.CommandText = "ALTER TABLE T1PLTHR ADD COLUMN PTCA INTEGER NOT NULL DEFAULT 0;";
                alterCmd.ExecuteNonQuery();
            }

            using (var updateCmd = connection.CreateCommand())
            {
                updateCmd.CommandText = "UPDATE T1PLTHR SET PTCA = 0 WHERE PTCA IS NULL;";
                updateCmd.ExecuteNonQuery();
            }
        }
    }
}
