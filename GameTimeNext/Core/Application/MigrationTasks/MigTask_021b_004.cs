using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;

namespace GameTimeNext.Core.Application.MigrationTasks
{
    public class MigTask_021b_004
    {
        public static void Execute()
        {
            var connection = AppEnvironment.GetDataBaseManager().GetConnection();

            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            bool hasArch = false;

            using (var checkCmd = connection.CreateCommand())
            {
                checkCmd.CommandText = "PRAGMA table_info(T1PROFI);";

                using (var reader = checkCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string columnName = reader["name"]?.ToString();

                        if (string.Equals(columnName, "ARCH", StringComparison.OrdinalIgnoreCase))
                        {
                            hasArch = true;
                            break;
                        }
                    }
                }
            }

            if (!hasArch)
            {
                using (var alterCmd = connection.CreateCommand())
                {
                    alterCmd.CommandText = "ALTER TABLE T1PROFI ADD COLUMN ARCH INTEGER NOT NULL DEFAULT 0;";
                    alterCmd.ExecuteNonQuery();
                }

                using (var updateCmd = connection.CreateCommand())
                {
                    updateCmd.CommandText = "UPDATE T1PROFI SET ARCH = 0 WHERE ARCH IS NULL;";
                    updateCmd.ExecuteNonQuery();
                }
            }

            using (var checkGroupCmd = connection.CreateCommand())
            {
                checkGroupCmd.CommandText = "SELECT COUNT(*) FROM T1GROUP WHERE GRNA = @grna AND GTYP = @gtyp;";
                var p1 = checkGroupCmd.CreateParameter();
                p1.ParameterName = "@grna";
                p1.Value = "Archived";
                checkGroupCmd.Parameters.Add(p1);

                var p2 = checkGroupCmd.CreateParameter();
                p2.ParameterName = "@gtyp";
                p2.Value = GroupType.Condition;
                checkGroupCmd.Parameters.Add(p2);

                object result = checkGroupCmd.ExecuteScalar();
                long count = 0;
                if (result != null && result != DBNull.Value)
                    count = Convert.ToInt64(result);

                if (count == 0)
                {
                    using var insertGroupCmd = connection.CreateCommand();
                    insertGroupCmd.CommandText = "INSERT INTO T1GROUP (GRNA, GTYP, CRAT, CHAT) VALUES (@grna, @gtyp, @crat, @chat);";
                    insertGroupCmd.Parameters.AddWithValue("@grna", "Archived");
                    insertGroupCmd.Parameters.AddWithValue("@gtyp", GroupType.Condition);
                    insertGroupCmd.Parameters.AddWithValue("@crat", DateTime.Today);
                    insertGroupCmd.Parameters.AddWithValue("@chat", DateTime.Now);
                    insertGroupCmd.ExecuteNonQuery();
                }
            }
        }
    }
}
