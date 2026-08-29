using GameTimeNext.Core.Framework.Config;
using System.Collections.ObjectModel;
using System.IO;

namespace GameTimeNext.Core.Framework.DataBase.Migration
{
    /// <summary>
    /// Used for centrally generating/migrating the database.
    /// </summary>
    internal static class MigrationFactory
    {
        // OFDOI: MigrationFactory
        // Store current schema in a file (maybe in DevSync directory)
        // Methods for:
        //   - Autogenerating new CreateDB-SQL
        //   - Autogenerating new Migration-SQLs
        //   - Applying changes from DevSync the same way, it gets applied while migrating (use same methods -> centralized)
        // Attention:
        //   - Maybe add default values in Metadata (true/false required for bools)

        private static Dictionary<string, List<List<string>>> _CACHED_FILECONTENTS = new Dictionary<string, List<List<string>>>();

        private static List<TableSchema>? _METADATA;
        private static List<TableSchema> METADATA
        {
            get
            {
                if (_METADATA is not null) return _METADATA;

                List<TableSchema> tableSchemas = new List<TableSchema>();

                TableSchema mH = new TableSchema("T1METAH");
                mH.AddColumn(new ColumnSchema("MENAM", "01", "0", "1", "1", "0"));
                mH.AddColumn(new ColumnSchema("DESCR", "01", "0", "2", "0", "0"));
                mH.AddColumn(new ColumnSchema("MTYPE", "01", "0", "3", "0", "0"));
                mH.AddColumn(new ColumnSchema("DSYNC", "06", "0", "4", "0", "0"));
                mH.AddColumn(new ColumnSchema("GENER", "06", "0", "5", "0", "0"));
                mH.AddColumn(new ColumnSchema("CRAT", "05", "0", "6", "0", "0"));
                mH.AddColumn(new ColumnSchema("CRUS", "01", "0", "7", "0", "0"));
                mH.AddColumn(new ColumnSchema("CHAT", "05", "0", "8", "0", "0"));
                mH.AddColumn(new ColumnSchema("CHUS", "01", "0", "9", "0", "0"));
                tableSchemas.Add(mH);

                TableSchema mP = new TableSchema("T1METAP");
                mP.AddColumn(new ColumnSchema("MENAM", "01", "0", "1", "1", "0"));
                mP.AddColumn(new ColumnSchema("PONAM", "01", "0", "2", "1", "0"));
                mP.AddColumn(new ColumnSchema("DESCR", "01", "0", "3", "0", "0"));
                mP.AddColumn(new ColumnSchema("DATYP", "01", "0", "4", "0", "0"));
                mP.AddColumn(new ColumnSchema("DALEN", "02", "0", "5", "0", "0"));
                mP.AddColumn(new ColumnSchema("PORDE", "02", "0", "6", "0", "0"));
                mP.AddColumn(new ColumnSchema("PRIMK", "06", "0", "7", "0", "0"));
                mP.AddColumn(new ColumnSchema("AUTOI", "06", "0", "8", "0", "0"));
                mP.AddColumn(new ColumnSchema("CRAT", "05", "0", "9", "0", "0"));
                mP.AddColumn(new ColumnSchema("CRUS", "01", "0", "10", "0", "0"));
                mP.AddColumn(new ColumnSchema("CHAT", "05", "0", "11", "0", "0"));
                mP.AddColumn(new ColumnSchema("CHUS", "01", "0", "12", "0", "0"));
                tableSchemas.Add(mP);

                _METADATA = tableSchemas;
                return _METADATA;
            }
        }

        #region Methods PUBLIC
        // OFDOI: Run these methods from DatabaseMigration and DevSync
        // Manual statements in MigTask and Initialization is deprecated!!
        public static string GetSqlMigration()
        {
            List<MigrationAction> actions = GenerateMigrationActions();
            return String.Join(Environment.NewLine, actions.Select(a => a.GetSql()));
        }

        public static string GetSqlCreate(bool metadata = false)
        {
            List<TableSchema> tableSchemas;
            if (metadata)
                tableSchemas = METADATA;
            else
                tableSchemas = GenerateTableSchemasFromDevSync();

            return String.Join(Environment.NewLine, tableSchemas.Select(s => s.GetSqlCreate()));
        }
        #endregion

        #region Methods PRIVATE
        private static List<TableSchema> GenerateTableSchemasFromStoredSchema()
        {
            string metahFilePath = Path.Combine(AppConfig.Dev.DevSyncDirectoryPath, "schema_T1METAH.csv");
            if (!File.Exists(metahFilePath)) return new List<TableSchema>();
            string metapFilePath = Path.Combine(AppConfig.Dev.DevSyncDirectoryPath, "schema_T1METAP.csv");
            if (!File.Exists(metapFilePath)) return new List<TableSchema>();

            return GenerateTableSchemas(metahFilePath, metapFilePath);
        }

        private static List<TableSchema> GenerateTableSchemasFromDevSync()
        {
            string metahFilePath = Path.Combine(AppConfig.Dev.DevSyncDirectoryPath, "T1METAH.csv");
            if (!File.Exists(metahFilePath)) return new List<TableSchema>();
            string metapFilePath = Path.Combine(AppConfig.Dev.DevSyncDirectoryPath, "T1METAP.csv");
            if (!File.Exists(metapFilePath)) return new List<TableSchema>();

            return GenerateTableSchemas(metahFilePath, metapFilePath);
        }

        private static List<TableSchema> GenerateTableSchemas(string metahFilePath, string metapFilePath)
        {
            List<TableSchema> tableSchemas = new List<TableSchema>();

            // Read files
            List<List<string>> metahLines = GetFileContent(metahFilePath);
            if (metahLines.Count.Equals(0)) return tableSchemas;
            List<List<string>> metapLines = GetFileContent(metapFilePath);
            if (metapLines.Count.Equals(0)) return tableSchemas;

            // Iterate tables
            bool headerSkipped = false;
            foreach (List<string> metahLine in metahLines)
            {
                if (!headerSkipped)
                {
                    headerSkipped = true;
                    continue;
                }

                string MENAM = metahLine[0];
                List<List<string>> filteredMetapLines = metapLines.FindAll(l => l[0].Equals(MENAM));
                if (filteredMetapLines.Count.Equals(0))
                    continue;

                TableSchema tableSchema = new TableSchema(MENAM);
                AddColumnSchemas(ref tableSchema, filteredMetapLines);

                tableSchemas.Add(tableSchema);
            }

            return tableSchemas;
        }

        private static List<List<string>> GetFileContent(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            List<List<string>>? contents = _CACHED_FILECONTENTS.GetValueOrDefault(fileInfo.Name);

            if (contents is null)
            {
                contents = File.ReadAllLines(filePath).Select(l => l.Split(';').ToList()).ToList();
                _CACHED_FILECONTENTS.Add(fileInfo.Name, contents);
            }

            return contents;
        }

        private static void AddColumnSchemas(ref TableSchema tableSchema, List<List<string>> metapLines)
        {
            // Iterate tables
            foreach (List<string> metapLine in metapLines)
            {
                ColumnSchema columnSchema = new ColumnSchema(metapLine[1], metapLine[3], metapLine[4], metapLine[5], metapLine[6], metapLine[7]);

                tableSchema.AddColumn(columnSchema);
            }
        }

        private static List<MigrationAction> GenerateMigrationActions()
        {
            List<TableSchema> tableSchemasDevSync = GenerateTableSchemasFromDevSync();
            List<TableSchema> tableSchemasStored = GenerateTableSchemasFromStoredSchema();

            Dictionary<string, MigrationAction> actions = new Dictionary<string, MigrationAction>();

            // CREATE
            tableSchemasDevSync.ForEach(s =>
            {
                actions.Add(s.MENAM, new MigrationAction()
                {
                    Type = ActionType.CREATE,
                    SchemaDevSync = s
                });
            });

            // NOTHING, ALTER & DROP
            tableSchemasStored.ForEach(s =>
            {
                MigrationAction? existingAction = actions.GetValueOrDefault(s.MENAM);

                if (existingAction is not null)
                {
                    // NOTHING & ALTER
                    existingAction.SchemaStored = s;

                    if (existingAction.GetAlterNeeded())
                        existingAction.Type = ActionType.ALTER; // ALTER
                    else
                        actions.Remove(s.MENAM); // NOTHING
                }
                else
                {
                    // DROP
                    actions.Add(s.MENAM, new MigrationAction()
                    {
                        Type = ActionType.DROP,
                        SchemaStored = s
                    });
                }
            });

            return actions.Select(a => a.Value).ToList();
        }
        #endregion

        #region Subclasses
        /// <summary>
        /// Defines what a table should look like.
        /// </summary>
        private class TableSchema
        {
            private static string MIGRATION_SUFFIX = "_mig";

            #region Properties
            public string MENAM { get; }
            public string MigrationTableName
            {
                get => $"{MENAM}{MIGRATION_SUFFIX}";
            }

            private List<ColumnSchema> _columns = new List<ColumnSchema>();
            public ReadOnlyCollection<ColumnSchema> Columns { get => new ReadOnlyCollection<ColumnSchema>(_columns); }
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

            public string GetSqlCreate()
            {
                return GetSqlCreate(false);
            }

            public string GetSqlDrop(bool migrationTable = false)
            {
                string migrationSuffix = migrationTable ? MIGRATION_SUFFIX : string.Empty;

                return $"DROP TABLE {MENAM}{migrationSuffix};";
            }

            public string GetMultiSqlAlter(TableSchema newTableSchema)
            {
                List<string> statements = new List<string>()
                {
                    GetSqlCreate(migrationTable: true),
                    GetSqlInsertInto(MigrationTableName),
                    GetSqlDrop(),
                    newTableSchema.GetSqlCreate(),
                    newTableSchema.GetSqlInsertInto(MigrationTableName, reverse: true),
                    GetSqlDrop(migrationTable: true)
                };

                return String.Join(Environment.NewLine, statements);
            }
            #endregion

            #region Methods PRIVATE
            private string GetSqlCreate(bool migrationTable)
            {
                string migrationSuffix = migrationTable ? MIGRATION_SUFFIX : string.Empty;

                List<string> sqlLines = new List<string>() {
                    $"CREATE TABLE IF NOT EXISTS {MENAM}{migrationSuffix} ("
                };

                List<ColumnSchema> columnsPK = _columns.FindAll(c => c.PRIMK.Equals(true)).ToList();

                // Columns
                for (int i = 0; i < _columns.Count; i++)
                {
                    if ((_columns.Count > 1 && (i < _columns.Count - 1)) || columnsPK.Count > 0)
                        sqlLines.Add($"{_columns[i].GetSql()},");
                    else
                        sqlLines.Add(_columns[i].GetSql());
                }

                // Primary Keys
                if (columnsPK.Count > 0)
                {
                    sqlLines.Add($"PRIMARY KEY ({String.Join(", ", columnsPK.Select(c => c.PONAM).ToList())})");
                }

                sqlLines.Add(");");

                return String.Join(Environment.NewLine, sqlLines);
            }

            private string GetSqlInsertInto(string targetTableName, bool reverse = false)
            {
                string sourceTableName = reverse ? targetTableName : MENAM;
                string actTargetTableName = reverse ? MENAM : targetTableName;

                return $"INSERT INTO {actTargetTableName} SELECT * FROM {sourceTableName};";
            }
            #endregion
        }

        /// <summary>
        /// Defines what a column should look like.
        /// </summary>
        private class ColumnSchema
        {
            #region Properties
            public string PONAM { get; }

            public string DATYP { get; }

            public int DALEN { get; }

            public int PORDE { get; }

            public bool PRIMK { get; }

            public bool AUTOI { get; }
            #endregion

            public ColumnSchema(string ponam, string datyp, string dalen, string porde, string primk, string autoi)
            {
                PONAM = ponam;
                DATYP = datyp;
                DALEN = Convert.ToInt32(dalen);
                PORDE = Convert.ToInt32(porde);
                PRIMK = primk.Equals("1");
                AUTOI = autoi.Equals("1");
            }

            #region Methods PUBLIC
            public string GetSql()
            {
                List<string> parts = new List<string>()
                {
                    PONAM,
                    SqliteDataType.GetByKey(DATYP).GetSqliteType(DALEN)
                };

                if (AUTOI) parts.Add("AUTO INCREMENT");

                return String.Join(' ', parts);
            }

            public override string ToString()
            {
                List<string> parts = new List<string>()
                {
                    PONAM,
                    DATYP,
                    DALEN.ToString("#"),
                    PORDE.ToString("#"),
                    PRIMK ? "1" : "0",
                    AUTOI ? "1" : "0"
                };

                return String.Join(";", parts);
            }
            #endregion
        }

        private enum ActionType
        {
            CREATE = 1,
            ALTER = 2,
            DROP = 9,
        }

        private class MigrationAction
        {
            #region Properties
            public ActionType Type { get; set; }
            
            public TableSchema? SchemaDevSync { get; set; }

            public TableSchema? SchemaStored { get; set; }
            #endregion

            public MigrationAction()
            {

            }

            #region Methods PUBLIC
            public bool GetAlterNeeded()
            {
                if (SchemaDevSync is null || SchemaStored is null) return false;

                // Column counts differ
                if (SchemaDevSync.Columns.Count != SchemaStored.Columns.Count) return true;

                // Column names differ
                string columnNamesDevSync = String.Join(';', SchemaDevSync.Columns.Select(c => c.PONAM)).ToLowerInvariant();
                string columnNamesStored = String.Join(';', SchemaStored.Columns.Select(c => c.PONAM)).ToLowerInvariant();
                if (!columnNamesDevSync.Equals(columnNamesStored)) return true;

                // Column definitions differ
                string columnDefinitionDevSync = String.Join(';', SchemaDevSync.Columns.Select(c => c.ToString())).ToLowerInvariant();
                string columnDefinitionStored = String.Join(';', SchemaStored.Columns.Select(c => c.ToString())).ToLowerInvariant();
                if (!columnDefinitionDevSync.Equals(columnDefinitionStored)) return true;

                return false;
            }

            public string GetSql()
            {
                switch (Type)
                {
                    case ActionType.CREATE:
                        return SchemaDevSync!.GetSqlCreate();
                    case ActionType.DROP:
                        return SchemaStored!.GetSqlDrop();
                    case ActionType.ALTER:
                        return SchemaStored!.GetMultiSqlAlter(SchemaDevSync!);
                    default:
                        throw new NotImplementedException("ActionType not yet implemented!");
                }
            }
            #endregion
        }

        private class SqliteDataType
        {
            private const string Integer = "INTEGER";
            private const string Real = "REAL";
            private const string DateTime = "DATETIME";
            private const string Text = "TEXT";
            private const string Varchar = "VARCHAR";

            private static List<SqliteDataType> _DATATYPES = new List<SqliteDataType>()
            {
                new SqliteDataType("01", "String", Text),
                new SqliteDataType("02", "Integer", Integer),
                new SqliteDataType("03", "Long", Integer),
                new SqliteDataType("04", "Double", Real),
                new SqliteDataType("05", "DateTime", DateTime),
                new SqliteDataType("06", "Boolean", Integer),
                new SqliteDataType("07", "MemoText", Text)
            };
            private static ReadOnlyCollection<SqliteDataType> DATATYPES = new ReadOnlyCollection<SqliteDataType>(_DATATYPES);

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
            public static SqliteDataType GetByKey(string key)
            {
                SqliteDataType? result = DATATYPES.Where(d => d.Key.ToLowerInvariant().Equals(key.ToLowerInvariant())).SingleOrDefault();
                if (result is null)
                    throw new InvalidDataException($"Unknown datatype of key \"{key}\".");

                return result;
            }

            public string GetSqliteType(int length = 0)
            {
                // TEXT, VARCHAR, VARCHAR(length)
                if (_sqliteType.Equals(Text) && !Key.Equals("07"))
                {
                    string temp = Varchar;

                    if (!length.Equals(0))
                    {
                        string lengthString = length.ToString("#");
                        temp += $"({lengthString})";
                    }

                    return temp;
                }

                // DATETIME
                // OFDOI: DATETIME

                return _sqliteType;
            }
            #endregion
        }
        #endregion
    }
}
