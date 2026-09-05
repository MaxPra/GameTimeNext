using System.Data.SQLite;

namespace GameTimeNext.Core.Framework.DataBase.Migration
{
    internal static partial class MigrationFactory
    {
        public static class Metadata
        {
            private static List<TableSchema>? _METADATA;
            public static List<TableSchema> METADATA
            {
                get
                {
                    if (_METADATA is not null) return _METADATA;

                    List<TableSchema> tableSchemas = new List<TableSchema>();

                    TableSchema mH = new TableSchema("T1METAH");
                    mH.AddColumn(new ColumnSchema("MENAM", "01", 0, 0, true, false, defak: false, defvl: ""));
                    mH.AddColumn(new ColumnSchema("DESCR", "01", 0, 1, false, false, defak: false, defvl: ""));
                    mH.AddColumn(new ColumnSchema("MTYPE", "01", 0, 2, false, false, defak: false, defvl: ""));
                    mH.AddColumn(new ColumnSchema("DSYNC", "06", 0, 3, false, false, defak: false, defvl: ""));
                    mH.AddColumn(new ColumnSchema("GENER", "06", 0, 4, false, false, defak: false, defvl: ""));
                    mH.AddColumn(new ColumnSchema("CRAT", "05", 0, 5, false, false, defak: false, defvl: ""));
                    mH.AddColumn(new ColumnSchema("CRUS", "01", 0, 6, false, false, defak: false, defvl: ""));
                    mH.AddColumn(new ColumnSchema("CHAT", "05", 0, 7, false, false, defak: false, defvl: ""));
                    mH.AddColumn(new ColumnSchema("CHUS", "01", 0, 8, false, false, defak: false, defvl: ""));
                    tableSchemas.Add(mH);

                    TableSchema mP = new TableSchema("T1METAP");
                    mP.AddColumn(new ColumnSchema("MENAM", "01", 0, 0, true, false, defak: false, defvl: ""));
                    mP.AddColumn(new ColumnSchema("PONAM", "01", 0, 1, true, false, defak: false, defvl: ""));
                    mP.AddColumn(new ColumnSchema("DESCR", "01", 0, 2, false, false, defak: false, defvl: ""));
                    mP.AddColumn(new ColumnSchema("DATYP", "01", 0, 3, false, false, defak: false, defvl: ""));
                    mP.AddColumn(new ColumnSchema("DALEN", "02", 0, 4, false, false, defak: false, defvl: ""));
                    mP.AddColumn(new ColumnSchema("PORDE", "02", 0, 5, false, false, defak: false, defvl: ""));
                    mP.AddColumn(new ColumnSchema("PRIMK", "06", 0, 6, false, false, defak: false, defvl: ""));
                    mP.AddColumn(new ColumnSchema("AUTOI", "06", 0, 7, false, false, defak: false, defvl: ""));
                    mP.AddColumn(new ColumnSchema("CRAT", "05", 0, 8, false, false, defak: false, defvl: ""));
                    mP.AddColumn(new ColumnSchema("CRUS", "01", 0, 9, false, false, defak: false, defvl: ""));
                    mP.AddColumn(new ColumnSchema("CHAT", "05", 0, 10, false, false, defak: false, defvl: ""));
                    mP.AddColumn(new ColumnSchema("CHUS", "01", 0, 11, false, false, defak: false, defvl: ""));
                    mP.AddColumn(new ColumnSchema("DEFAK", "06", 0, 12, false, false, defak: false, defvl: ""));
                    mP.AddColumn(new ColumnSchema("DEFVL", "01", 0, 13, false, false, defak: false, defvl: ""));
                    tableSchemas.Add(mP);

                    _METADATA = tableSchemas;
                    return _METADATA;
                }
            }

            public static void MigrateTables(SQLiteConnection? connection = null)
            {
                if (connection is null)
                    connection = AppEnvironment.GetDataBaseManager().GetConnection();

                List<TableSchema> tableSchemasBefore = SchemaGenerator.GenerateFromActualDatabase(connection);
                List<MigrationAction> actions = MigrationActionGenerator.Generate(connection, tableSchemasBefore, metadata: true);

                actions.ForEach(a => a.Migrate(connection));
            }
        }
    }
}
