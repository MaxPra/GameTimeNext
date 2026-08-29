using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.DataBase.Migration;
using System.Data;
using System.Data.SQLite;
using System.IO;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Framework.DataBase
{
    class DataBaseManager
    {

        private SQLiteConnection _connection;

        public DataBaseManager() { }

        // [------------------------------------------------]
        // [------------------ PUBLIC ----------------------]
        // [------------------------------------------------]

        public void Initialize()
        {
            Initialize(AppConfig.Storage.DatabaseFilePath);
        }

        /// <summary>
        /// Initialisiert die Datenbankverbindung und legt die Datenbank an, sollte sie noch nicht existieren
        /// </summary>
        public void Initialize(string databaseFilePath)
        {
            // -- Erstellen --
            // Prüfen, ob File existiert
            if (File.Exists(databaseFilePath))
            {
                // -- Verbinden --
                ConnectToSQLite(databaseFilePath);

                return;
            }

            // -- Verbinden --
            ConnectToSQLite(databaseFilePath);

            // -- Tabellen erstellen --
            CreateMetadataTables();

        }

        public void CreateBackup(string backupPathInklFileName)
        {

            using var destinationConnection = new SQLiteConnection($"Data Source={backupPathInklFileName};Version=3;");

            ConnectToSQLite(AppConfig.Storage.DatabaseFilePath);

            try
            {
                destinationConnection.Open();

                _connection.BackupDatabase(
                    destinationConnection,
                    "main",
                    "main",
                    -1,
                    null,
                    0);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (destinationConnection != null)
                    destinationConnection.Close();
            }
        }

        /// <summary>
        /// Liefert die Connection zur SQLite Datenbank
        /// </summary>
        /// <returns></returns>
        public SQLiteConnection GetConnection()
        {
            return _connection;
        }

        public void EnsureT1groupSeeded()
        {
            Dictionary<string, int> oldIds = new Dictionary<string, int>();
            GetT1groupIds(ref oldIds);

            bool migrationNeeded = false;
            if (!oldIds.ContainsKey("External"))
                migrationNeeded = true;
            if (!migrationNeeded && !oldIds["External"].Equals("6"))
                migrationNeeded = true;

            if (migrationNeeded)
                ClearT1group();

            EnsureDefaultValuesT1groupConditions();
            EnsureDefaultValuesT1groupTags();

            if (migrationNeeded)
                UpdateT1grppo(oldIds);
        }

        // [------------------------------------------------]
        // [------------------ PRIVATE ---------------------]
        // [------------------------------------------------]

        private bool ConnectToSQLite(string databaseFilePath)
        {

            if (_connection != null && _connection.State == ConnectionState.Open)
                return true;

            bool newDataBase = false;
            string connectionString = String.Empty;

            if (!File.Exists(databaseFilePath))
            {
                connectionString = $"Data Source={databaseFilePath};Version=3;New=True;Compress=True;BusyTimeout=15000;Pooling=False;";
                newDataBase = true;
            }
            else
            {
                connectionString = $"Data Source={databaseFilePath};Version=3;Compress=True;BusyTimeout=15000;Pooling=False;";
                newDataBase = false;
            }

            _connection = new SQLiteConnection(connectionString);
            try { _connection.Open(); } catch { }

            try
            {
                using var fkOn = new SQLiteCommand("PRAGMA foreign_keys = ON;", _connection);
                fkOn.ExecuteNonQuery();
            }
            catch { }

            return newDataBase;
        }

        /// <summary>
        /// Erstellt alle Tabellen, sofern noch nicht vorhanden (Erster Start)
        /// </summary>
        private void CreateMetadataTables()
        {
            string sql = MigrationFactory.GetSqlCreate(metadata: true);
            UIXQuery.ExecuteCustom(sql, GetConnection());
        }

        private void GetT1groupIds(ref Dictionary<string, int> dictionary)
        {
            string sql = "SELECT GRID, GRNA FROM T1GROUP;";

            using (var reader = UIXQuery.QueryCustom(sql, _connection))
            {
                while (reader.Read())
                {
                    int GRID = UIXQuery.GetInt32(reader, "GRID");
                    string GRNA = UIXQuery.GetString(reader, "GRNA");

                    dictionary[GRNA] = GRID;
                }
                reader.Close();
            }
        }

        private void ClearT1group()
        {
            List<string> sqlLines = new List<string>() {
                "PRAGMA foreign_keys = OFF;",
                "DELETE FROM T1GROUP;",
                "PRAGMA foreign_keys = ON;"
            };
            UIXQuery.ExecuteCustom(String.Join(Environment.NewLine, sqlLines), _connection);
        }

        private void UpdateT1grppo(Dictionary<string, int> oldIds)
        {
            Dictionary<string, int> newIds = new Dictionary<string, int>();
            GetT1groupIds(ref newIds);

            foreach (var oldId in oldIds)
            {
                string sql;
                if (newIds.TryGetValue(oldId.Key, out int newValue))
                {
                    sql = $"UPDATE T1GRPPO SET GRID='{newValue}' WHERE GRID='{oldId.Value}';";
                }
                else
                {
                    sql = $"DELETE FROM T1GRPPO WHERE GRID='{oldId.Value}';";
                }

                UIXQuery.ExecuteCustom(sql, _connection);
            }
        }

        private void EnsureDefaultValuesT1groupConditions()
        {
            var sql = @"INSERT OR REPLACE INTO T1GROUP (GRID, GRNA, GTYP, CRAT, CHAT)
                        VALUES 
                        (1, 'Completed', @gtyp, @crat, @chat),
                        (2, 'Unplayed', @gtyp, @crat, @chat),
                        (3, 'Currently Playing', @gtyp, @crat, @chat),
                        (4, 'Playable', @gtyp, @crat, @chat),
                        (5, 'Archived', @gtyp, @crat, @chat),
                        (6, 'External', @gtyp, @crat, @chat);";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            command.Parameters.AddWithValue("@gtyp", GroupType.Condition);
            command.Parameters.AddWithValue("@crat", DateTime.Today);
            command.Parameters.AddWithValue("@chat", DateTime.Now);

            command.ExecuteNonQuery();
        }

        private void EnsureDefaultValuesT1groupTags()
        {
            var sql = @"
                        INSERT OR REPLACE INTO T1GROUP (GRID, GRNA, GTYP, CRAT, CHAT)
                        VALUES
                        (101, 'Singleplayer', @gtyp, @crat, @chat),
                        (102, 'Multiplayer', @gtyp, @crat, @chat),
                        (103, 'Co-op', @gtyp, @crat, @chat),
                        (104, 'PvP', @gtyp, @crat, @chat),

                        (105, 'Action', @gtyp, @crat, @chat),
                        (106, 'Adventure', @gtyp, @crat, @chat),
                        (107, 'RPG', @gtyp, @crat, @chat),
                        (108, 'Strategy', @gtyp, @crat, @chat),
                        (109, 'Simulation', @gtyp, @crat, @chat),
                        (110, 'Shooter', @gtyp, @crat, @chat),
                        (111, 'Horror', @gtyp, @crat, @chat),
                        (112, 'Survival', @gtyp, @crat, @chat),

                        (113, 'Open World', @gtyp, @crat, @chat),
                        (114, 'Sandbox', @gtyp, @crat, @chat),
                        (115, 'Story Rich', @gtyp, @crat, @chat),
                        (116, 'Exploration', @gtyp, @crat, @chat),
                        (117, 'Crafting', @gtyp, @crat, @chat),
                        (118, 'Building', @gtyp, @crat, @chat),

                        (119, 'First Person', @gtyp, @crat, @chat),
                        (120, 'Third Person', @gtyp, @crat, @chat),
                        (121, 'Isometric', @gtyp, @crat, @chat),
                        (122, 'Top-Down', @gtyp, @crat, @chat);
                    ";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            command.Parameters.AddWithValue("@gtyp", GroupType.Tag);
            command.Parameters.AddWithValue("@crat", DateTime.Today);
            command.Parameters.AddWithValue("@chat", DateTime.Now);

            command.ExecuteNonQuery();
        }
    }
}
