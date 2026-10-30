using System.Data.SQLite;
using System.Globalization;
using System.Text.Json.Nodes;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.MigrationTasks
{
    /// <summary>
    /// Moves the Steam link of the profiles into the generic launcher link:
    /// T1PROFI.SAID -> LNTY = "STEAM" / LNID = app ID, and PRSE "SteamGameArgs" (spaces stored as ";") -> "LaunchArgs".
    /// Runs on every start (only touches profiles that weren't migrated yet), because profiles migrated from
    /// GameTimeX still bring the old settings. Remove it together with the field SAID.
    /// </summary>
    internal class MigTask_100b_010 : MigTaskBase
    {
        public MigTask_100b_010() : base("1.0.0", requireDb: true, executeAlways: true)
        {

        }

        protected override void ExecuteImpl()
        {
            MigrateSteamAppIds();
            MigrateSteamGameArgs();
        }

        private void MigrateSteamAppIds()
        {
            using SQLiteCommand command = _connection!.CreateCommand();
            command.CommandText = "UPDATE T1PROFI SET LNTY = 'STEAM', LNID = CAST(SAID AS TEXT) " +
                                  "WHERE SAID <> 0 AND IFNULL(LNTY, '') = '';";
            int migratedProfiles = command.ExecuteNonQuery();

            if (migratedProfiles > 0)
                FnLog.AddInfo(nameof(MigTask_100b_010), $"Steam link of {migratedProfiles.ToString(CultureInfo.InvariantCulture)} profile(s) moved to the launcher link");
        }

        private void MigrateSteamGameArgs()
        {
            Dictionary<long, string> migratedSettings = new Dictionary<long, string>();

            using (SQLiteCommand select = _connection!.CreateCommand())
            {
                select.CommandText = "SELECT PFID, PRSE FROM T1PROFI WHERE PRSE LIKE '%SteamGameArgs%';";

                using SQLiteDataReader reader = select.ExecuteReader();
                while (reader.Read())
                {
                    long pfid = reader.GetInt64(0);

                    try
                    {
                        if (JsonNode.Parse(reader.GetString(1)) is not JsonObject settings)
                            continue;

                        string steamGameArgs = settings["SteamGameArgs"]?.GetValue<string>() ?? string.Empty;
                        settings.Remove("SteamGameArgs");

                        if (settings["LaunchArgs"] is null)
                            settings["LaunchArgs"] = steamGameArgs.Replace(";", " ").Trim();

                        migratedSettings[pfid] = settings.ToJsonString();
                    }
                    catch (Exception ex)
                    {
                        // Broken settings of one profile must not stop the startup; the profile keeps its old settings
                        FnLog.AddWarning(nameof(MigTask_100b_010), $"Could not migrate the Steam launch arguments of profile {pfid.ToString(CultureInfo.InvariantCulture)}", ex);
                    }
                }
            }

            foreach (KeyValuePair<long, string> migratedSetting in migratedSettings)
            {
                using SQLiteCommand update = _connection.CreateCommand();
                update.CommandText = "UPDATE T1PROFI SET PRSE = @PRSE WHERE PFID = @PFID;";
                update.Parameters.AddWithValue("@PRSE", migratedSetting.Value);
                update.Parameters.AddWithValue("@PFID", migratedSetting.Key);
                update.ExecuteNonQuery();
            }
        }
    }
}
