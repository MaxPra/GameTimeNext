using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.SelectionLists;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.Utils;
using static GameTimeNext.Core.Application.Profiles.Controller.ProfilesViewController;

namespace GameTimeNext.Core.Application.MigrationTasks
{
    /// <summary>
    /// Tags move from T1GROUP (GTYP = GTN.TAG, numeric GRID) into codetable tG (string keys):
    /// the delivered tags 101..122 become "D_101".."D_122", tags the user had created get a generated "U_" key.
    /// Links (T1GRPPO) get the key in TGKY, and the tag filters stored in the config (ExcludedTagIds) become ExcludedTagKeys.
    /// Runs on every start but only touches what wasn't migrated yet, so it also copes with a codetable that arrives late
    /// (import package) and with filters restored from an old config.
    /// </summary>
    internal class MigTask_100b_012 : MigTaskBase
    {
        private const string DeliveredKeyPrefix = "D_";
        private const int FirstDeliveredGrid = 101;

        /// <summary>
        /// Names of the delivered tags in the order of their former GRIDs (101, 102, ...). Only used to create a missing entry.
        /// </summary>
        private static readonly string[] DeliveredTagNames =
        {
            "Singleplayer", "Multiplayer", "Co-op", "PvP", "Action", "Adventure", "RPG", "Strategy", "Simulation", "Shooter", "Horror", "Survival",
            "Open World", "Sandbox", "Story Rich", "Exploration", "Crafting", "Building", "First Person", "Third Person", "Isometric", "Top-Down"
        };

        public MigTask_100b_012() : base("1.0.0", requireDb: true, executeAlways: true)
        {

        }

        protected override void ExecuteImpl()
        {
            Dictionary<long, string> keyByGrid = MigrateTagLinks();

            MigrateFilterCaches(keyByGrid);
        }

        #region Tags and links
        /// <summary>
        /// Returns the key of every former tag GRID that was migrated (empty if there is nothing to do).
        /// </summary>
        private Dictionary<long, string> MigrateTagLinks()
        {
            Dictionary<long, string> keyByGrid = new Dictionary<long, string>();

            Dictionary<long, string> legacyTags = ReadLegacyTags();

            if (legacyTags.Count == 0 && !HasUnmigratedDeliveredLinks())
                return keyByGrid;

            // The entries come with the import package (release) or the DevSync CSV (debug): without the codetable there is nothing to link to
            if (!CodetableExists())
            {
                FnLog.AddWarning("MainApp", $"Tags can't be migrated yet: codetable {TFCTABD.TagCodetable} is missing (import package not imported?)");
                return keyByGrid;
            }

            TXCTABD txctabd = new TXCTABD();
            List<T1CTABD> entries = txctabd.GetEntries(TFCTABD.TagCodetable);

            // A former tag is the same tag as the entry of the same name (delivered ones first); its old GRID decides otherwise
            foreach ((long grid, string name) in legacyTags)
                keyByGrid[grid] = ResolveKey(txctabd, entries, grid, name);

            // Links to delivered tags whose T1GROUP row is already gone
            for (int i = 0; i < DeliveredTagNames.Length; i++)
            {
                long grid = FirstDeliveredGrid + i;

                if (!keyByGrid.ContainsKey(grid))
                    keyByGrid[grid] = EnsureDeliveredEntry(txctabd, entries, grid);
            }

            using SQLiteTransaction transaction = _connection!.BeginTransaction();

            foreach ((long grid, string key) in keyByGrid)
            {
                using SQLiteCommand update = _connection.CreateCommand();
                update.Transaction = transaction;
                update.CommandText = "UPDATE T1GRPPO SET TGKY = @key, GRID = 0 WHERE GRID = @grid AND (TGKY IS NULL OR TGKY = '');";
                update.Parameters.AddWithValue("@key", key);
                update.Parameters.AddWithValue("@grid", grid);
                update.ExecuteNonQuery();
            }

            // Two former tags that became the same entry can leave a profile with the link twice
            using (SQLiteCommand dedupe = _connection.CreateCommand())
            {
                dedupe.Transaction = transaction;
                dedupe.CommandText = @"DELETE FROM T1GRPPO WHERE TGKY <> '' AND GPID NOT IN
                                       (SELECT MIN(GPID) FROM T1GRPPO WHERE TGKY <> '' GROUP BY PFID, TGKY);";
                dedupe.ExecuteNonQuery();
            }

            using (SQLiteCommand delete = _connection.CreateCommand())
            {
                delete.Transaction = transaction;
                delete.CommandText = "DELETE FROM T1GROUP WHERE GTYP = @gtyp;";
                delete.Parameters.AddWithValue("@gtyp", GroupType.Tag);
                delete.ExecuteNonQuery();
            }

            transaction.Commit();

            FnLog.AddInfo("MainApp", $"{legacyTags.Count.ToString(CultureInfo.InvariantCulture)} tag(s) migrated to codetable {TFCTABD.TagCodetable}");

            return keyByGrid;
        }

        private Dictionary<long, string> ReadLegacyTags()
        {
            Dictionary<long, string> legacyTags = new Dictionary<long, string>();

            using SQLiteCommand command = _connection!.CreateCommand();
            command.CommandText = "SELECT GRID, GRNA FROM T1GROUP WHERE GTYP = @gtyp ORDER BY GRID;";
            command.Parameters.AddWithValue("@gtyp", GroupType.Tag);

            using SQLiteDataReader reader = command.ExecuteReader();

            while (reader.Read())
                legacyTags[reader.GetInt64(0)] = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);

            return legacyTags;
        }

        private bool HasUnmigratedDeliveredLinks()
        {
            using SQLiteCommand command = _connection!.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM T1GRPPO WHERE (TGKY IS NULL OR TGKY = '') AND GRID BETWEEN @first AND @last;";
            command.Parameters.AddWithValue("@first", FirstDeliveredGrid);
            command.Parameters.AddWithValue("@last", FirstDeliveredGrid + DeliveredTagNames.Length - 1);

            return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        private bool CodetableExists()
        {
            using SQLiteCommand command = _connection!.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM T1CTABH WHERE TXTYP = @txtyp;";
            command.Parameters.AddWithValue("@txtyp", TFCTABD.TagCodetable);

            return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        private static string ResolveKey(TXCTABD txctabd, List<T1CTABD> entries, long grid, string name)
        {
            T1CTABD? sameName = entries
                .Where(e => string.Equals(e.DESCR, name, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(e => e.TXNUM.StartsWith(DeliveredKeyPrefix, StringComparison.Ordinal))
                .FirstOrDefault();

            if (sameName is not null)
                return sameName.TXNUM;

            if (grid >= FirstDeliveredGrid && grid < FirstDeliveredGrid + DeliveredTagNames.Length)
                return EnsureDeliveredEntry(txctabd, entries, grid);

            // A tag of the user: becomes an entry of the user's list
            return CreateEntry(txctabd, entries, CFSelectionListsApp.GenerateKey(TFCTABD.TagCodetable), name);
        }

        private static string EnsureDeliveredEntry(TXCTABD txctabd, List<T1CTABD> entries, long grid)
        {
            string key = DeliveredKeyPrefix + grid.ToString(CultureInfo.InvariantCulture);

            if (entries.Any(e => e.TXNUM == key))
                return key;

            return CreateEntry(txctabd, entries, key, DeliveredTagNames[grid - FirstDeliveredGrid]);
        }

        private static string CreateEntry(TXCTABD txctabd, List<T1CTABD> entries, string key, string name)
        {
            T1CTABD entry = txctabd.CreateNew();
            entry.TXTYP = TFCTABD.TagCodetable;
            entry.TXNUM = key;
            entry.DESCR = name;
            entry.PARM1 = "N";
            txctabd.Save(entry);

            entries.Add(entry);

            return key;
        }
        #endregion

        #region Filters of the config
        private static void MigrateFilterCaches(Dictionary<long, string> keyByGrid)
        {
            bool changed = MigrateFilterCache(AppEnvironment.GetAppConfig().FilterCache, keyByGrid);
            changed |= MigrateFilterCache(AppEnvironment.GetAppConfig().BigPictureFilterCache, keyByGrid);

            if (changed)
                AppEnvironment.SaveAppConfig();
        }

        private static bool MigrateFilterCache(FilterCache filter, Dictionary<long, string> keyByGrid)
        {
            if (filter.LegacyExcludedTagIds is null)
                return false;

            foreach (long grid in filter.LegacyExcludedTagIds)
            {
                string? key = null;

                if (keyByGrid.TryGetValue(grid, out string? mapped))
                    key = mapped;
                else if (grid >= FirstDeliveredGrid && grid < FirstDeliveredGrid + DeliveredTagNames.Length)
                    key = DeliveredKeyPrefix + grid.ToString(CultureInfo.InvariantCulture);

                // A tag that doesn't exist any more can't be excluded either
                if (key is not null && !filter.ExcludedTagKeys.Contains(key))
                    filter.ExcludedTagKeys.Add(key);
            }

            filter.LegacyExcludedTagIds = null;

            return true;
        }
        #endregion
    }
}
