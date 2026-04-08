using GameTimeNext.Core.Framework;
using System.IO;

namespace GameTimeNext.Core.Application.MigrationTasks
{
    public class MigTask_026b_005
    {
        public static void Execute()
        {

            var connection = AppEnvironment.GetDataBaseManager().GetConnection();

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

            DeleteUnusedProfileImages();
        }

        private static void DeleteUnusedProfileImages()
        {
            string coverFolder = AppEnvironment.GetAppConfig().CoverFolderPath;

            if (!Directory.Exists(coverFolder))
                return;

            var connection = AppEnvironment.GetDataBaseManager().GetConnection();
            var referencedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT PPFN FROM T1PROFI WHERE PPFN IS NOT NULL AND PPFN != '';";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var ppfn = reader["PPFN"]?.ToString();
                        if (!string.IsNullOrEmpty(ppfn))
                            referencedFiles.Add(ppfn);
                    }
                }
            }

            foreach (var filePath in Directory.EnumerateFiles(coverFolder))
            {
                string fileName = Path.GetFileName(filePath);
                if (!referencedFiles.Contains(fileName))
                    File.Delete(filePath);
            }
        }
    }
}
