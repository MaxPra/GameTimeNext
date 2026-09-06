using GameTimeNext.Core.Application.Metadata.Data;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
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
                List<ColumnSchema> filteredColumns = Columns.ToList();
                if (filteringSchema is not null)
                    filteredColumns = Columns.Where(c => filteringSchema.Columns.Where(fC => fC.PONAM.Equals(c.PONAM)).SingleOrDefault() is not null).ToList();

                return String.Join(", ", filteredColumns.Select(c => c.PONAM));
            }

            public string GetSql_Create()
            {
                List<string> sqlLines = new List<string>() {
                    $"CREATE TABLE IF NOT EXISTS {MENAM} ("
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

            public string GetSql_Drop()
            {
                return $"DROP TABLE {MENAM};";
            }

            public string GetSql_AlterName(string newTableName)
            {
                string temp = $"ALTER TABLE {MENAM} RENAME TO {newTableName};";
                MENAM = newTableName;
                return temp;
            }

            public string GetSql_InsertInto(TableSchema sourceSchema)
            {
                string columnNames = sourceSchema.GetColumnNamesForSql(this);

                return $"INSERT INTO {MENAM} ({columnNames}) SELECT {columnNames} FROM {sourceSchema.MENAM};";
            }
            #endregion
        }

        public class ColumnSchema
        {
            #region Properties
            public string PONAM { get; }

            public SqliteDataType DATYP { get; }

            public int DALEN { get; }

            public int PORDE { get; }

            public bool PRIMK { get; }

            public bool AUTOI { get; }

            public bool DEFAK { get; }

            public string DEFVL { get; }
            #endregion

            public ColumnSchema(string ponam, string datyp, int dalen, int porde, bool primk, bool autoi, bool defak = false, string defvl = "")
            {
                PONAM = ponam;
                DATYP = SqliteDataType.GetByKey(datyp);
                DALEN = dalen;
                PORDE = porde;
                PRIMK = primk;
                AUTOI = autoi;
                DEFAK = defak;
                DEFVL = defvl;
            }

            #region Methods PUBLIC
            public string GetSql(int countPk)
            {
                List<string> parts = new List<string>()
                {
                    PONAM,
                    DATYP.GetSqliteType(DALEN)
                };

                if (PRIMK && countPk == 1) parts.Add("PRIMARY KEY");
                if (AUTOI && countPk <= 1) parts.Add("AUTOINCREMENT");

                // OFDOI: Add defaults
                if (DATYP.Equals("06")) parts.Add("DEFAULT '0'");

                return String.Join(' ', parts);
            }

            public string ToCsvString()
            {
                List<string> parts = new List<string>()
                {
                    PONAM,
                    DATYP.Key,
                    DALEN.ToString("#"),
                    PORDE.ToString("#"),
                    PRIMK ? "1" : "0",
                    AUTOI ? "1" : "0",
                    DEFAK ? "1" : "0",
                    DEFVL,
                };

                return String.Join(";", parts);
            }
            #endregion

            #region Methods PRIVATE
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
                            UIXQuery.ExecuteCustom(SchemaBefore!.GetSql_AlterName(SchemaBefore!.MENAM_mig), connection);
                            UIXQuery.ExecuteCustom(SchemaAfter!.GetSql_Create(), connection);
                            UIXQuery.ExecuteCustom(SchemaAfter!.GetSql_InsertInto(SchemaBefore), connection);
                            UIXQuery.ExecuteCustom(SchemaBefore!.GetSql_Drop(), connection);
                            break;
                        default:
                            throw new NotImplementedException("ActionType not yet implemented!");
                    }

                    UIXQuery.ExecuteCustom(_SQL_TRANSACTION_COMMIT, connection);
                }
                catch
                {
                    UIXQuery.ExecuteCustom(_SQL_TRANSACTION_ROLLBACK, connection);
                    return false;
                }
                finally
                {
                    UIXQuery.ExecuteCustom(_SQL_PRIMARYKEY_ON, connection);
                }

                return true;
            }
            #endregion

            #region Methods PRIVATE
            #endregion
        }

        public class SqliteDataType
        {
            private static class SqliteString
            {
                public const string Integer = "INTEGER";
                public const string Real = "REAL";
                public const string DateTime = "DATETIME";
                public const string Text = "TEXT";
                public const string Varchar = "VARCHAR";
                public const string Boolean = "BOOLEAN";
            }

            private static ReadOnlyCollection<SqliteDataType> DATATYPES = new ReadOnlyCollection<SqliteDataType>(new List<SqliteDataType> ()
                {
                    new SqliteDataType("01", "String", SqliteString.Text),
                    new SqliteDataType("02", "Integer", SqliteString.Integer),
                    new SqliteDataType("03", "Long", SqliteString.Integer),
                    new SqliteDataType("04", "Double", SqliteString.Real),
                    new SqliteDataType("05", "DateTime", SqliteString.DateTime),
                    new SqliteDataType("06", "Boolean", SqliteString.Boolean),
                    new SqliteDataType("07", "MemoText", SqliteString.Text),
                }
            );

            #region Properties
            public string Key { get; }

            public string Name { get; }

            private string _sqliteType { get; }
            #endregion

            private SqliteDataType(string key, string name, string sqliteType)
            {
                Key = key;
                Name = name;
                _sqliteType = sqliteType;
            }

            #region Methods PUBLIC
            public string GetSqliteType(int length = 0)
            {
                // TEXT, VARCHAR, VARCHAR(length)
                if (_sqliteType.Equals(SqliteString.Text) && !Key.Equals("07"))
                {
                    string temp = SqliteString.Varchar;

                    if (!length.Equals(0))
                    {
                        string lengthString = length.ToString("#");
                        temp += $"({lengthString})";
                    }

                    return temp;
                }

                return _sqliteType;
            }

            public static SqliteDataType GetByKey(string key)
            {
                SqliteDataType? result = DATATYPES.Where(d => d.Key.ToLowerInvariant().Equals(key.ToLowerInvariant())).SingleOrDefault();
                if (result is null)
                    throw new InvalidDataException($"Unknown datatype of key \"{key}\".");

                return result;
            }

            public static SqliteDataType GetBySqliteType(string name)
            {
                string typeName = name.Split(' ').First().Split('(').First();

                switch (typeName)
                {
                    case SqliteString.Text:
                        return DATATYPES.Where(t => t.Key.Equals("07")).Single();
                    case SqliteString.Varchar:
                        return DATATYPES.Where(t => t.Key.Equals("01")).Single();
                    case SqliteString.Integer:
                        return DATATYPES.Where(t => t.Key.Equals("02")).Single();
                    case SqliteString.Real:
                        return DATATYPES.Where(t => t.Key.Equals("04")).Single();
                    case SqliteString.DateTime:
                        return DATATYPES.Where(t => t.Key.Equals("05")).Single();
                    case SqliteString.Boolean:
                        return DATATYPES.Where(t => t.Key.Equals("06")).Single();
                    default:
                        throw new NotImplementedException();
                }
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
                        txmetah.Read(tableName!)
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
                            defvl: t1metap.DEFVL
                        ));
                    }

                    schemas.Add(tS);
                }

                return schemas;
            }

            public static List<TableSchema> GenerateFromActualDatabase(SQLiteConnection connection)
            {
                // OFDOI: parse CREATE statement instead of PRAGMA table_info

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

                            ColumnSchema cS = new ColumnSchema(name, SqliteDataType.GetBySqliteType(type).Key, 0, order, pk, false);
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
                    if (metadata)
                    {
                        if (!tS.MENAM.Equals("T1METAH") && !tS.MENAM.Equals("T1METAP")) return;
                    }
                    else
                    {
                        if (tS.MENAM.Equals("T1METAH") || tS.MENAM.Equals("T1METAP")) return;
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

        private static class SqliteCsvDataConverter
        {
            public static string DateToCsv(string value)
            {
                DateTimeStyles styles = DateTimeStyles.AllowWhiteSpaces;
                string[] formats =
                {
                    "yyyy-MM-dd HH:mm:ss",
                    "yyyy-MM-dd HH:mm:ss.FFFFFFF",
                    "yyyy-MM-ddTHH:mm:ss",
                    "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
                    "dd.MM.yyyy HH:mm:ss",
                    "dd.MM.yyyy HH:mm",
                    "o"
                };

                if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, styles, out DateTime exactResult) || DateTime.TryParse(value, CultureInfo.InvariantCulture, styles, out exactResult))
                    return exactResult.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                return value;
            }

            public static string EscapeCsv(string value, char separator)
            {
                if (value is null)
                    return string.Empty;

                bool mustQuote = value.Contains(separator) || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
                if (!mustQuote)
                    return value;

                return '"' + value.Replace("\"", "\"\"") + '"';
            }

            public static string ToCsvValue(object value)
            {
                if (value is null || value == DBNull.Value)
                    return string.Empty;

                if (value is bool)
                    return (bool)value ? "1" : "0";

                if (value is IFormattable formattable)
                    return formattable.ToString(null, CultureInfo.InvariantCulture);

                return value.ToString() ?? string.Empty;
            }
        }
        #endregion
    }
}
