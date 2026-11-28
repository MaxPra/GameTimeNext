using Arcora.Core.Framework;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.Querying;

namespace Arcora.Core.Application.Metadata.Data
{
    /// <summary>
    /// Migration journal of the database (T1SYMIG): which migration tasks and import packages were applied, and the
    /// version that was started last. Used by the startup only (no application), so the data manager is created with new.
    /// </summary>
    public static class TFSYMIG
    {
        /// <summary>
        /// True if the table exists in the database. It is created by the migration of the hard-coded tables, so it is
        /// missing in a database that was last opened by a version without the journal.
        /// </summary>
        public static bool TableExists()
        {
            using SQLiteCommand cmd = AppEnvironment.GetDataBaseManager().GetConnection().CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @NAME;";
            cmd.Parameters.AddWithValue("@NAME", K1SYMIG.Name);
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        /// <summary>
        /// True if the entry was applied to this database.
        /// </summary>
        public static bool IsApplied(string type, string id)
        {
            return new TXSYMIG().Exists(type, id);
        }

        /// <summary>
        /// Records the entry as applied by the running version. An entry that is already recorded keeps its first
        /// version and time.
        /// </summary>
        public static void MarkApplied(string type, string id)
        {
            TXSYMIG txsymig = new TXSYMIG();
            if (txsymig.Exists(type, id)) return;

            T1SYMIG t1symig = txsymig.CreateNew();
            t1symig.MGTY = type;
            t1symig.MGID = id;
            t1symig.MGAV = AppEnvironment.AppVersion.InformationalVersion;
            txsymig.Save(t1symig);
        }

        /// <summary>
        /// IDs of all applied entries of a type (e.g. the class names of the applied migration tasks).
        /// </summary>
        public static HashSet<string> GetApplied(string type)
        {
            UIXQuery query = new UIXQuery(K1SYMIG.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddField(K1SYMIG.Name, K1SYMIG.Fields.MGID);
            query.AddWhere(K1SYMIG.Name, K1SYMIG.Fields.MGTY, QueryCompareType.EQUALS, type);

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            using var reader = query.Execute();
            while (reader.Read())
                ids.Add(UIXQuery.GetString(reader, K1SYMIG.Name, K1SYMIG.Fields.MGID, def: ""));

            return ids;
        }

        /// <summary>
        /// InformationalVersion that was started last with this database, null if none was recorded yet.
        /// </summary>
        public static string? GetLastStartedVersion()
        {
            return new TXSYMIG().Read(EntryType.Version, VersionKey.LastStarted)?.MGAV;
        }

        /// <summary>
        /// Records the running version as the version started last.
        /// </summary>
        public static void SetLastStartedVersion()
        {
            string version = AppEnvironment.AppVersion.InformationalVersion;

            TXSYMIG txsymig = new TXSYMIG();
            T1SYMIG t1symig = txsymig.Read(EntryType.Version, VersionKey.LastStarted) ?? txsymig.CreateNew();
            if (t1symig.MGAV == version) return;

            t1symig.MGTY = EntryType.Version;
            t1symig.MGID = VersionKey.LastStarted;
            t1symig.MGAV = version;
            txsymig.Save(t1symig);
        }

        /// <summary>
        /// Entry types (MGTY). Stored values, so they never change with a rename in code.
        /// </summary>
        public static class EntryType
        {
            /// <summary>Migration task, ID = class name.</summary>
            public const string Task = "TASK";

            /// <summary>Import package, ID = package ID.</summary>
            public const string Package = "PACKAGE";

            /// <summary>Version entry, ID = one of <see cref="VersionKey"/>.</summary>
            public const string Version = "VERSION";
        }

        /// <summary>
        /// IDs (MGID) of the version entries.
        /// </summary>
        public static class VersionKey
        {
            /// <summary>Holds the version started last (MGAV).</summary>
            public const string LastStarted = "LAST_STARTED";
        }
    }
}
