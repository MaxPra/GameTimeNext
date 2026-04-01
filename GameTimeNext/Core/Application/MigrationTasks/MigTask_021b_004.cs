using System;
using System.IO;
using System.Collections.Generic;
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

            // Kürze in allen T1PROFI-Einträgen das Feld PPFN auf den Dateinamen (ohne Pfad)
            var updates = new List<(long RowId, string Ppfn)>();

            using (var selectCmd = connection.CreateCommand())
            {
                // Ensure a stable column name for the row identifier across different SQLite provider behaviors
                selectCmd.CommandText = "SELECT ROWID AS _ROWID, PPFN FROM T1PROFI;";

                using (var reader = selectCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        object rowObj = reader["_ROWID"];
                        if (rowObj == null || rowObj == DBNull.Value)
                            continue;

                        long rowid = Convert.ToInt64(rowObj);
                        var ppfnObj = reader["PPFN"];
                        var ppfn = ppfnObj?.ToString();
                        if (string.IsNullOrEmpty(ppfn))
                            continue;

                        var fileName = Path.GetFileName(ppfn);
                        if (!string.Equals(ppfn, fileName, StringComparison.Ordinal))
                            updates.Add((rowid, fileName ?? string.Empty));
                    }
                }
            }

            foreach (var u in updates)
            {
                using var updateCmd = connection.CreateCommand();
                updateCmd.CommandText = "UPDATE T1PROFI SET PPFN = @ppfn WHERE ROWID = @rowid;";
                updateCmd.Parameters.AddWithValue("@ppfn", u.Ppfn);
                updateCmd.Parameters.AddWithValue("@rowid", u.RowId);
                updateCmd.ExecuteNonQuery();
            }
        }
    }
}
