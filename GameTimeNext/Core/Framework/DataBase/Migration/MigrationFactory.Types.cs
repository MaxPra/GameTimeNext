using GameTimeNext.Core.Application.Metadata.Data;
using GameTimeNext.Core.Framework.Utils;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using UIX.ViewController.Engine.Migration.Types;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.DataBase.Migration
{
    internal static partial class MigrationFactory
    {
        public class TableSchema
        {
            #region Properties
            public string MENAM { get; private set; }
            public string MENAM_mig
            {
                get => $"{MENAM}_mig";
            }

            private List<ColumnSchema> _columns = new List<ColumnSchema>();
            public ReadOnlyCollection<ColumnSchema> Columns { get => new ReadOnlyCollection<ColumnSchema>(_columns); }

            public bool IsCodetableTable
            {
                get => "T1CTABH".Equals(MENAM, StringComparison.OrdinalIgnoreCase) || "T1CTABD".Equals(MENAM, StringComparison.OrdinalIgnoreCase);
            }

            public bool IsCodetableTabd
            {
                get => "T1CTABD".Equals(MENAM, StringComparison.OrdinalIgnoreCase);
            }

            public bool IsMetadataTable
            {
                get => MENAM.StartsWith("T1META");
            }
            #endregion

            public TableSchema(string menam)
            {
                MENAM = menam;
            }

            #region Methods PUBLIC
            public void AddColumn(ColumnSchema column)
            {
                ColumnSchema? existingColumn = _columns.Find(c => c.PONAM.Equals(column.PONAM));
                if (existingColumn is not null)
                    throw new InvalidOperationException("ColumnSchema already exists in TableSchema!");

                _columns.Add(column);
            }

            /// <summary>
            /// Returns an SQL formatted list of its column names.
            /// </summary>
            /// <returns>e.g.: col1, col2, col3, ...</returns>
            public string GetColumnNamesForSql()
            {
                return GetColumnNamesForSql(null);
            }

            /// <summary>
            /// Returns an SQL formatted list of its column names.
            /// </summary>
            /// <param name="filteringSchema">Return values will only include columns, which are also present in this schema.</param>
            /// <returns>e.g.: col1, col2, col3, ...</returns>
            public string GetColumnNamesForSql(TableSchema? filteringSchema)
            {
                List<ColumnSchema> filteredColumns = GetFilteredColumns(filteringSchema);

                return String.Join(", ", filteredColumns.Select(c => c.PONAM));
            }

            public string GetColumnNamesWithCoalesceForSql(TableSchema? filteringSchema)
            {
                List<ColumnSchema> filteredColumns = GetFilteredColumns(filteringSchema);

                return String.Join(", ", filteredColumns.Select(c => $"COALESCE({c.PONAM}, {c.GetDefaultValue()})"));
            }

            public string GetSql_Create(string? tableNameOverride = null)
            {
                List<string> sqlLines = new List<string>() {
                    $"CREATE TABLE IF NOT EXISTS {tableNameOverride ?? MENAM} ("
                };

                List<ColumnSchema> columnsPK = _columns.FindAll(c => c.PRIMK.Equals(true)).ToList();
                int countPk = columnsPK.Count;

                // Columns
                for (int i = 0; i < _columns.Count; i++)
                {
                    if ((_columns.Count > 1 && (i < _columns.Count - 1)) || countPk > 1)
                        sqlLines.Add($"{_columns[i].GetSql(countPk)},");
                    else
                        sqlLines.Add(_columns[i].GetSql(countPk));
                }

                // Primary Keys
                if (columnsPK.Count > 1)
                {
                    sqlLines.Add($"PRIMARY KEY ({String.Join(", ", columnsPK.Select(c => c.PONAM).ToList())})");
                }

                sqlLines.Add(");");

                return String.Join(Environment.NewLine, sqlLines);
            }

            public string GetSql_Drop(string? tableNameOverride = null)
            {
                return $"DROP TABLE {tableNameOverride ?? MENAM};";
            }

            public string GetSql_DropIfExists(string? tableNameOverride = null)
            {
                return $"DROP TABLE IF EXISTS {tableNameOverride ?? MENAM};";
            }

            public string GetSql_RenameTo(string sourceTableName)
            {
                return $"ALTER TABLE {sourceTableName} RENAME TO {MENAM};";
            }

            public string GetSql_InsertInto(TableSchema sourceSchema, string? tableNameOverride = null)
            {
                string columnNames = sourceSchema.GetColumnNamesForSql(this);
                string columnNamesWithCoalesce = sourceSchema.GetColumnNamesWithCoalesceForSql(this);

                return $"INSERT INTO {tableNameOverride ?? MENAM} ({columnNames}) SELECT {columnNamesWithCoalesce} FROM {sourceSchema.MENAM};";
            }
            #endregion

            #region Methods PRIVATE
            private List<ColumnSchema> GetFilteredColumns(TableSchema? filteringSchema)
            {
                if (filteringSchema is not null)
                    return Columns.Where(c => filteringSchema.Columns.Where(fC => fC.PONAM.Equals(c.PONAM)).SingleOrDefault() is not null).ToList();

                return Columns.ToList();
            }
            #endregion
        }

        public class ColumnSchema
        {
            #region Properties
            public string PONAM { get; }

            public UIXSqliteDataType DATYP { get; }

            public int DALEN { get; }

            public int PORDE { get; }

            public bool PRIMK { get; }

            public bool AUTOI { get; }

            public bool DEFAK { get; }

            public string DEFVL { get; }

            public string? CHAT { get; }
            #endregion

            public ColumnSchema(string ponam, string datyp, int dalen, int porde, bool primk, bool autoi, bool defak = false, string defvl = "", string? chat = null)
            {
                PONAM = ponam;
                DATYP = UIXSqliteDataType.GetByKey(datyp);
                DALEN = dalen;
                PORDE = porde;
                PRIMK = primk;
                AUTOI = autoi;
                DEFAK = defak;
                DEFVL = defvl;
                CHAT = chat;
            }

            #region Methods PUBLIC
            public string GetSql(int countPk)
            {
                List<string> parts = new List<string>()
                {
                    PONAM,
                    DATYP.GetSqliteType(DALEN),
                    "NOT NULL"
                };

                if (PRIMK && countPk == 1) parts.Add("PRIMARY KEY");
                if (AUTOI && countPk <= 1) parts.Add("AUTOINCREMENT");

                parts.Add($"DEFAULT {GetDefaultValue()}");

                return String.Join(' ', parts);
            }

            public string ToCsvString()
            {
                List<string> parts = new List<string>()
                {
                    PONAM,
                    DATYP.Key,
                    ToCsvValue(DALEN),
                    ToCsvValue(PORDE),
                    ToCsvValue(PRIMK),
                    ToCsvValue(AUTOI),
                    ToCsvValue(DEFAK),
                    DEFVL,
                };

                return String.Join(";", parts);
            }

            public string GetDefaultValue(bool forCsharp = false)
            {
                string sD = "'"; // String delimiter
                if (forCsharp)
                    sD = $"\"";

                if (DEFAK)
                {
                    // Default set by Metadata (DEFVL is stored in CSV format)
                    if (DATYP.Key.Equals("06") && forCsharp)
                        return UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.CSV, UIXSqliteDataType.ConversionType.CSharp, DEFVL, DATYP) is true ? "true" : "false";

                    object? valueSqlite = UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.CSV, UIXSqliteDataType.ConversionType.Sqlite, DEFVL, DATYP);
                    string valueString = Convert.ToString(valueSqlite, CultureInfo.InvariantCulture) ?? string.Empty;

                    return $"{sD}{valueString}{sD}";
                }
                else
                {
                    // Default not set -> Fallback
                    if (DATYP.Key.Equals("01")) return $"{sD}{sD}";
                    if (DATYP.Key.Equals("02")) return "0";
                    if (DATYP.Key.Equals("03")) return "0";
                    if (DATYP.Key.Equals("04")) return "0";
                    if (DATYP.Key.Equals("05"))
                    {
                        if (forCsharp) return "DateTime.MinValue";

                        object? value = UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.CSharp, UIXSqliteDataType.ConversionType.Sqlite, DateTime.MinValue);
                        return $"{sD}{value}{sD}";
                    }
                    if (DATYP.Key.Equals("06"))
                    {
                        if (forCsharp) return "false";

                        object? value = UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.CSharp, UIXSqliteDataType.ConversionType.Sqlite, false);
                        return $"{sD}{value}{sD}";
                    }
                    if (DATYP.Key.Equals("07")) return $"{sD}{sD}";
                }

                throw new NotImplementedException($"Not default value implemented for SqliteDataType with Key \"{DATYP.Key}\".");
            }
            #endregion

            #region Methods PRIVATE
            private static string ToCsvValue(object? valFrom)
            {
                return (string)UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.CSharp, UIXSqliteDataType.ConversionType.CSV, valFrom)!;
            }
            #endregion
        }

        private class MigrationAction
        {
            #region Properties
            public MigrationActionType Type { get; set; }

            public TableSchema? SchemaBefore { get; set; }

            public TableSchema? SchemaAfter { get; set; }

            public bool NeedsAlter
            {
                get
                {
                    if (SchemaAfter is null || SchemaBefore is null) return false;

                    // Column counts differ
                    if (SchemaAfter.Columns.Count != SchemaBefore.Columns.Count) return true;

                    // Column names differ
                    string columnNamesAfter = String.Join(';', SchemaAfter.Columns.Select(c => c.PONAM)).ToLowerInvariant();
                    string columnNamesBefore = String.Join(';', SchemaBefore.Columns.Select(c => c.PONAM)).ToLowerInvariant();
                    if (!columnNamesAfter.Equals(columnNamesBefore)) return true;

                    // Column definitions differ
                    string columnDefinitionAfter = String.Join(';', SchemaAfter.Columns.Select(c => c.ToCsvString())).ToLowerInvariant();
                    string columnDefinitionBefore = String.Join(';', SchemaBefore.Columns.Select(c => c.ToCsvString())).ToLowerInvariant();
                    if (!columnDefinitionAfter.Equals(columnDefinitionBefore)) return true;

                    // Check CHAT as last indicator of change
                    if (!SchemaAfter.IsMetadataTable)
                    {
                        string chatsBefore = String.Join(';', SchemaBefore.Columns.Select(c => c.CHAT));
                        string chatsAfter = String.Join(';', SchemaAfter.Columns.Select(c => c.CHAT));
                        if (!chatsAfter.Equals(chatsBefore)) return true;
                    }

                    // In debug-mode always
                    if (FnSystem.IsDebug() && !FnSystem.RunWithUserPermissions()) return true;

                    return false;
                }
            }
            #endregion

            public MigrationAction(MigrationActionType type, TableSchema? schemaBefore, TableSchema? schemaAfter)
            {
                Type = type;
                SchemaBefore = schemaBefore;
                SchemaAfter = schemaAfter;
            }

            #region Methods PUBLIC
            public bool Migrate(SQLiteConnection connection)
            {
                LogInfo($"Migrating {(SchemaBefore ?? SchemaAfter)!.MENAM} ({Type.ToString()})...", subSystem: "MigrationAction", method: "Migrate");
                UIXQuery.ExecuteCustom(_SQL_PRIMARYKEY_OFF, connection);
                UIXQuery.ExecuteCustom(_SQL_TRANSACTION_BEGIN, connection);

                try
                {
                    switch (Type)
                    {
                        case MigrationActionType.CREATE:
                            UIXQuery.ExecuteCustom(SchemaAfter!.GetSql_Create(), connection);
                            break;
                        case MigrationActionType.DROP:
                            UIXQuery.ExecuteCustom(SchemaBefore!.GetSql_Drop(), connection);
                            break;
                        case MigrationActionType.ALTER:
                            // Build the new table under a temporary name and rename it only at the end (procedure from the SQLite docs).
                            // The old table must not be renamed: SQLite would otherwise rewrite REFERENCES of other tables to the temporary name.
                            UIXQuery.ExecuteCustom(SchemaAfter!.GetSql_DropIfExists(SchemaAfter!.MENAM_mig), connection);
                            UIXQuery.ExecuteCustom(SchemaAfter!.GetSql_Create(SchemaAfter!.MENAM_mig), connection);
                            UIXQuery.ExecuteCustom(SchemaAfter!.GetSql_InsertInto(SchemaBefore!, SchemaAfter!.MENAM_mig), connection);
                            UIXQuery.ExecuteCustom(SchemaBefore!.GetSql_Drop(), connection);
                            UIXQuery.ExecuteCustom(SchemaAfter!.GetSql_RenameTo(SchemaAfter!.MENAM_mig), connection);
                            break;
                        default:
                            throw new NotImplementedException("ActionType not yet implemented!");
                    }

                    UIXQuery.ExecuteCustom(_SQL_TRANSACTION_COMMIT, connection);
                    LogInfo($"Migrated {(SchemaBefore ?? SchemaAfter)!.MENAM} ({Type.ToString()})...", subSystem: "MigrationAction", method: "Migrate");
                }
                catch (Exception ex)
                {
                    UIXQuery.ExecuteCustom(_SQL_TRANSACTION_ROLLBACK, connection);
                    LogError($"Migration failed for {(SchemaBefore ?? SchemaAfter)!.MENAM} ({Type.ToString()})...", exception: ex, subSystem: "MigrationAction", method: "Migrate");
                    return false;
                }
                finally
                {
                    UIXQuery.ExecuteCustom(_SQL_PRIMARYKEY_ON, connection);
                }

                return true;
            }
            #endregion
        }

        private enum MigrationActionType
        {
            CREATE = 1,
            ALTER = 2,
            DROP = 3,
        }

        public enum ImportType
        {
            DevSync = 0,
            ImportPackages = 1,
            MetadataGenerator = 2,
        }

        public class ImportPackageType
        {
            public const string Codetables = "cT";
            public const string Metadata = "mE";
        }

        #region STATIC
        private static class SchemaGenerator
        {
            public static TableSchema GenerateSingleFromMetadata(string tableName)
            {
                return GenerateFromMetadata(tableName: tableName).Single();
            }

            public static List<TableSchema> GenerateFromMetadata(string? tableName = null, SQLiteConnection? connection = null)
            {
                List<TableSchema> schemas = new List<TableSchema>();

                if (!FnString.IsNullEmptyOrWhitespace(tableName) && tableName!.StartsWith("T1META"))
                {
                    // Metadata
                    schemas.Add(Metadata.METADATA.Where(s => s.MENAM.Equals(tableName)).Single());
                    return schemas;
                }

                TXMETAH txmetah;
                TXMETAP txmetap;
                if (connection is null)
                {
                    txmetah = new TXMETAH();
                    txmetap = new TXMETAP();

                }
                else
                {
                    txmetah = new TXMETAH(connection);
                    txmetap = new TXMETAP(connection);
                }

                List<T1METAH> t1metahs;
                if (FnString.IsNullEmptyOrWhitespace(tableName))
                    t1metahs = txmetah.ReadAll();
                else
                    t1metahs = new List<T1METAH>()
                    {
                        txmetah.Read(tableName!) ?? throw new InvalidOperationException($"No metadata found for '{tableName}'.")
                    };

                foreach (T1METAH t1metah in t1metahs)
                {
                    TableSchema tS = new TableSchema(t1metah.MENAM);

                    List<T1METAP> t1metaps = txmetap.ReadAll(t1metah.MENAM);
                    foreach (T1METAP t1metap in t1metaps)
                    {
                        tS.AddColumn(new ColumnSchema(
                            t1metap.PONAM,
                            t1metap.DATYP,
                            t1metap.DALEN,
                            t1metap.PORDE,
                            t1metap.PRIMK,
                            t1metap.AUTOI,
                            defak: t1metap.DEFAK,
                            defvl: t1metap.DEFVL,
                            chat: (string?)UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.CSharp, UIXSqliteDataType.ConversionType.CSV, t1metap.CHAT)
                        ));
                    }

                    schemas.Add(tS);
                }

                return schemas;
            }

            public static List<TableSchema> GenerateFromActualDatabase(SQLiteConnection connection)
            {
                // OFDO (#OF001): parse CREATE statement instead of PRAGMA table_info

                List<string> tableNames = new List<string>();
                using (var reader = UIXQuery.QueryCustom("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;", connection))
                {
                    while (reader.Read())
                    {
                        string name = UIXQuery.GetString(reader, "name");
                        if (name.StartsWith("T1"))
                            tableNames.Add(name);
                    }
                    reader.Close();
                }

                List<TableSchema> tableSchemas = new List<TableSchema>();
                foreach (string tableName in tableNames)
                {
                    TableSchema tS = new TableSchema(tableName);

                    using (var reader = UIXQuery.QueryCustom($"PRAGMA table_info({tableName});", connection))
                    {
                        int order = 0;
                        while (reader.Read())
                        {
                            string name = UIXQuery.GetString(reader, "name");
                            string type = UIXQuery.GetString(reader, "type");
                            string dflt = UIXQuery.GetString(reader, "dflt_value");
                            bool pk = UIXQuery.GetBool(reader, "pk");

                            ColumnSchema cS = new ColumnSchema(name, UIXSqliteDataType.GetBySqliteType(type).Key, 0, order, pk, false);
                            tS.AddColumn(cS);
                            order++;
                        }
                        reader.Close();
                    }

                    tableSchemas.Add(tS);
                }

                return tableSchemas;
            }
        }

        private static class MigrationActionGenerator
        {
            public static List<MigrationAction> Generate(SQLiteConnection connection, List<TableSchema> tableSchemasBefore, bool metadata = false)
            {
                List<TableSchema> tableSchemasNow;
                if (metadata)
                    tableSchemasNow = Metadata.METADATA;
                else
                    tableSchemasNow = SchemaGenerator.GenerateFromMetadata(connection: connection);

                Dictionary<string, MigrationAction> actions = new Dictionary<string, MigrationAction>();

                // CREATE
                tableSchemasNow.ForEach(tS => actions.Add(tS.MENAM, new MigrationAction(MigrationActionType.CREATE, null, tS)));

                // NOTHING, ALTER & DROP
                tableSchemasBefore.ForEach(tS =>
                {
                    if (metadata && !tS.IsMetadataTable)
                    {
                        return;
                    }
                    else if (!metadata && tS.IsMetadataTable)
                    {
                        return;
                    }

                    MigrationAction? existingAction = actions.GetValueOrDefault(tS.MENAM);
                    if (existingAction is not null)
                    {
                        // NOTHING & ALTER
                        existingAction.SchemaBefore = tS;

                        if (existingAction.NeedsAlter)
                            existingAction.Type = MigrationActionType.ALTER; // ALTER
                        else
                            actions.Remove(tS.MENAM); // NOTHING
                    }
                    else
                    {
                        // DROP
                        actions.Add(tS.MENAM, new MigrationAction(MigrationActionType.DROP, tS, null));
                    }
                });

                return actions.Select(a => a.Value).ToList();
            }
        }
        #endregion
    }
}
