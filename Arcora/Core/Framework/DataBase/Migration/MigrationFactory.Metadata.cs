using Arcora.Core.Framework.Utils;
using Arcora.Core.Framework.Versioning;
using System.Data.SQLite;
using UIX.ViewController.Engine.Utils;

namespace Arcora.Core.Framework.DataBase.Migration
{
    internal static partial class MigrationFactory
    {
        public static partial class Metadata
        {
            private static string _LAST_CHANGED_AT_VERSION = "1.0.0";

            private static List<TableSchema>? _METADATA;
            public static List<TableSchema> METADATA
            {
                get
                {
                    if (_METADATA is not null) return _METADATA;

                    List<TableSchema> tableSchemas = new List<TableSchema>();

                    TableSchema mH = new TableSchema("T1METAH");
                    mH.AddColumn(new ColumnSchema("MENAM", "01", 0, 10, true, false, defak: false, defvl: ""));
                    mH.AddColumn(new ColumnSchema("DESCR", "01", 0, 20, false, false, defak: false, defvl: ""));
                    mH.AddColumn(new ColumnSchema("MTYPE", "01", 0, 30, false, false, defak: false, defvl: ""));
                    mH.AddColumn(new ColumnSchema("DSYNC", "06", 0, 40, false, false, defak: true, defvl: "0"));
                    mH.AddColumn(new ColumnSchema("GENER", "06", 0, 50, false, false, defak: true, defvl: "0"));
                    mH.AddColumn(new ColumnSchema("CRAT", "05", 0, 60, false, false, defak: false, defvl: ""));
                    mH.AddColumn(new ColumnSchema("CRUS", "01", 0, 70, false, false, defak: false, defvl: ""));
                    mH.AddColumn(new ColumnSchema("CHAT", "05", 0, 80, false, false, defak: false, defvl: ""));
                    mH.AddColumn(new ColumnSchema("CHUS", "01", 0, 90, false, false, defak: false, defvl: ""));
                    tableSchemas.Add(mH);

                    TableSchema mP = new TableSchema("T1METAP");
                    mP.AddColumn(new ColumnSchema("MENAM", "01", 0, 10, true, false, defak: false, defvl: ""));
                    mP.AddColumn(new ColumnSchema("PONAM", "01", 0, 20, true, false, defak: false, defvl: ""));
                    mP.AddColumn(new ColumnSchema("DESCR", "01", 0, 30, false, false, defak: false, defvl: ""));
                    mP.AddColumn(new ColumnSchema("DATYP", "01", 0, 40, false, false, defak: false, defvl: ""));
                    mP.AddColumn(new ColumnSchema("DALEN", "02", 0, 50, false, false, defak: false, defvl: ""));
                    mP.AddColumn(new ColumnSchema("PORDE", "02", 0, 60, false, false, defak: false, defvl: ""));
                    mP.AddColumn(new ColumnSchema("PRIMK", "06", 0, 70, false, false, defak: true, defvl: "0"));
                    mP.AddColumn(new ColumnSchema("AUTOI", "06", 0, 80, false, false, defak: true, defvl: "0"));
                    mP.AddColumn(new ColumnSchema("DEFAK", "06", 0, 90, false, false, defak: true, defvl: "0"));
                    mP.AddColumn(new ColumnSchema("DEFVL", "01", 0, 100, false, false, defak: false, defvl: ""));
                    mP.AddColumn(new ColumnSchema("CRAT", "05", 0, 110, false, false, defak: false, defvl: ""));
                    mP.AddColumn(new ColumnSchema("CRUS", "01", 0, 120, false, false, defak: false, defvl: ""));
                    mP.AddColumn(new ColumnSchema("CHAT", "05", 0, 130, false, false, defak: false, defvl: ""));
                    mP.AddColumn(new ColumnSchema("CHUS", "01", 0, 140, false, false, defak: false, defvl: ""));
                    tableSchemas.Add(mP);

                    // Migration journal: what was applied to this database (migration tasks, import packages, last started version).
                    // Holds the state of one database, so it is never exported (see IsCsvExported).
                    TableSchema mJ = new TableSchema("T1SYMIG");
                    mJ.AddColumn(new ColumnSchema("MGTY", "01", 0, 10, true, false, defak: false, defvl: ""));  // Entry type: TASK, PACKAGE, VERSION
                    mJ.AddColumn(new ColumnSchema("MGID", "01", 0, 20, true, false, defak: false, defvl: ""));  // Task class name, package ID or version key
                    mJ.AddColumn(new ColumnSchema("MGAV", "01", 0, 30, false, false, defak: false, defvl: "")); // InformationalVersion of the app that wrote the entry
                    mJ.AddColumn(new ColumnSchema("CRAT", "05", 0, 40, false, false, defak: false, defvl: ""));
                    mJ.AddColumn(new ColumnSchema("CRUS", "01", 0, 50, false, false, defak: false, defvl: ""));
                    mJ.AddColumn(new ColumnSchema("CHAT", "05", 0, 60, false, false, defak: false, defvl: ""));
                    mJ.AddColumn(new ColumnSchema("CHUS", "01", 0, 70, false, false, defak: false, defvl: ""));
                    tableSchemas.Add(mJ);

                    _METADATA = tableSchemas;
                    return _METADATA;
                }
            }

            public static void MigrateTables(SQLiteConnection? connection = null)
            {
                LogInfo("Running...", subSystem: "Metadata", method: "MigrateTables");
                bool migrationNeeded = true;
                string versionConfigString = AppEnvironment.GetAppConfig().AppVersion;
                if (!FnString.IsNullEmptyOrWhitespace(versionConfigString))
                {
                    AppVersion versionConfig = new AppVersion();
                    versionConfig.Get(versionConfigString);

                    AppVersion versionRunning = AppEnvironment.AppVersion;

                    migrationNeeded = versionRunning.IsEqualOrBiggerThan(_LAST_CHANGED_AT_VERSION) && versionRunning.IsBiggerThan(versionConfig);
                }
                if ((!FnSystem.IsDebug() || FnSystem.RunWithUserPermissions()) && !migrationNeeded && !AppEnvironment.StartArguments.ContainsKey("force-metadata-migration")) return;
                LogInfo("Migrating...", subSystem: "Metadata", method: "MigrateTables");

                if (connection is null)
                    connection = AppEnvironment.GetDataBaseManager().GetConnection();

                List<TableSchema> tableSchemasBefore = SchemaGenerator.GenerateFromActualDatabase(connection);
                List<MigrationAction> actions = MigrationActionGenerator.Generate(connection, tableSchemasBefore, metadata: true);

                actions.ForEach(a => a.Migrate(connection));
                LogInfo("Migrated!", subSystem: "Metadata", method: "MigrateTables");
            }

            public static bool IsDefined(string? tableName)
            {
                if (FnString.IsNullEmptyOrWhitespace(tableName)) return false;
                return METADATA.Any(s => s.MENAM.Equals(tableName, StringComparison.OrdinalIgnoreCase));
            }

            /// <summary>
            /// True for the tables of METADATA whose data is exported as CSV (DevSync and import packages), currently
            /// T1METAH and T1METAP. A table is only added here deliberately: tables holding the state of one database
            /// (T1SYMIG, the migration journal) must never be exported, so IsDefined is not used for this.
            /// </summary>
            public static bool IsCsvExported(string? tableName)
            {
                return "T1METAH".Equals(tableName, StringComparison.OrdinalIgnoreCase)
                    || "T1METAP".Equals(tableName, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
