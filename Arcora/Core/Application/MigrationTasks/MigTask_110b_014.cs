using Arcora.Core.Framework;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.Utils;
using static Arcora.Core.Application.Profiles.Controller.ProfilesViewController;

namespace Arcora.Core.Application.MigrationTasks
{
    /// <summary>
    /// The stored keys with the prefix "GTN." get the prefix "ARCORA.": the estimated time type of the profiles (T1PROFI.ETTY),
    /// the group type of the conditions (T1GROUP.GTYP), the excluded filter options of both filter caches and the backup type
    /// of the config. Former tag rows (GTN.TAG) are left alone, MigTask_100b_012 migrates them into the codetable.
    /// Runs on every start (only touches values that still have the old prefix), because the version check ignores the
    /// beta tag (configs already on 1.1.0-beta would be skipped) and an imported backup brings its old data back.
    /// </summary>
    internal class MigTask_110b_014 : MigTaskBase
    {
        private const string FormerPrefix = "GTN.";
        private const string NewPrefix = "ARCORA.";

        public MigTask_110b_014() : base("1.1.0", requireDb: true, executeAlways: true)
        {

        }

        protected override void ExecuteImpl()
        {
            int migratedRows = MigrateColumn("T1PROFI", "ETTY", "GTN.EST_TIME.%");
            migratedRows += MigrateColumn("T1GROUP", "GTYP", "GTN.CONDITION");

            if (migratedRows > 0)
                FnLog.AddInfo("MainApp", $"Prefix of {migratedRows.ToString(CultureInfo.InvariantCulture)} stored key(s) changed from {FormerPrefix} to {NewPrefix}");

            MigrateConfig();
        }

        private int MigrateColumn(string table, string column, string pattern)
        {
            using SQLiteCommand command = _connection!.CreateCommand();
            command.CommandText = $"UPDATE {table} SET {column} = @newPrefix || substr({column}, @formerLength + 1) WHERE {column} LIKE @pattern;";
            command.Parameters.AddWithValue("@newPrefix", NewPrefix);
            command.Parameters.AddWithValue("@formerLength", FormerPrefix.Length);
            command.Parameters.AddWithValue("@pattern", pattern);

            return command.ExecuteNonQuery();
        }

        private static void MigrateConfig()
        {
            bool changed = MigrateFilterCache(AppEnvironment.GetAppConfig().FilterCache);
            changed |= MigrateFilterCache(AppEnvironment.GetAppConfig().BigPictureFilterCache);

            string? backupType = AppEnvironment.GetAppConfig().AppSettings.BackupType;

            if (HasFormerPrefix(backupType))
            {
                AppEnvironment.GetAppConfig().AppSettings.BackupType = ReplacePrefix(backupType!);
                changed = true;
            }

            if (!changed)
                return;

            AppEnvironment.SaveAppConfig();
            FnLog.AddInfo("MainApp", $"Prefix of the stored keys in the config changed from {FormerPrefix} to {NewPrefix}");
        }

        private static bool MigrateFilterCache(FilterCache filter)
        {
            bool changed = false;

            for (int i = 0; i < filter.ExcludedOptions.Count; i++)
            {
                if (!HasFormerPrefix(filter.ExcludedOptions[i]))
                    continue;

                filter.ExcludedOptions[i] = ReplacePrefix(filter.ExcludedOptions[i]);
                changed = true;
            }

            return changed;
        }

        private static bool HasFormerPrefix(string? value)
        {
            return value?.StartsWith(FormerPrefix, StringComparison.Ordinal) == true;
        }

        private static string ReplacePrefix(string value)
        {
            return NewPrefix + value.Substring(FormerPrefix.Length);
        }
    }
}
