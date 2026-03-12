using GameTimeNext.Core.Application.TableObjects;
using System.Data.SQLite;
using System.IO;

namespace GameTimeNext.Core.Framework.DataBase
{
    class DataBaseManager
    {

        private SQLiteConnection _connection;

        public DataBaseManager() { }

        // [------------------------------------------------]
        // [------------------ PUBLIC ----------------------]
        // [------------------------------------------------]

        /// <summary>
        /// Initialisiert die Datenbankverbindung und legt die Datenbank an, sollte sie noch nicht existieren
        /// </summary>
        public void Initialize()
        {
            // -- Erstellen --
            // Prüfen, ob File existiert
            if (File.Exists(AppEnvironment.GetAppConfig().DataBaseFilePath))
            {
                // -- Verbinden --
                ConnectToSQLite();

                return;
            }

            using (File.Create(AppEnvironment.GetAppConfig().DataBaseFilePath)) { }

            // -- Verbinden --
            ConnectToSQLite();

            // -- Tabellen erstellen --
            CreateTables();

        }

        /// <summary>
        /// Liefert die Connection zur SQLite Datenbank
        /// </summary>
        /// <returns></returns>
        public SQLiteConnection GetConnection()
        {
            return _connection;
        }

        // [------------------------------------------------]
        // [------------------ PRIVATE ---------------------]
        // [------------------------------------------------]

        private bool ConnectToSQLite()
        {
            bool newDataBase = false;
            string connectionString = String.Empty;

            if (!File.Exists(AppEnvironment.GetAppConfig().DataBaseFilePath))
            {
                connectionString = $"Data Source={AppEnvironment.GetAppConfig().DataBaseFilePath};Version=3;New=True;Compress=True;BusyTimeout=15000;Pooling=False;";
                newDataBase = true;
            }
            else
            {
                connectionString = $"Data Source={AppEnvironment.GetAppConfig().DataBaseFilePath};Version=3;Compress=True;BusyTimeout=15000;Pooling=False;";
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
        private void CreateTables()
        {
            // -- TAB_PROFI (Tabelle für Profile) --
            CreateTableTAB_PROFI();

            // -- TAB_SESSI (Tabelle für Sessions) --
            CreateTableTAB_SESSI();

            // -- TAB_GROUP (Tabelle für Gruppen) --
            CreateTableTAB_GROUP();
            InsertDefaultValuesTAB_GROUPConditions();
            InsertDefaultValuesTAB_GROUPTags();

            // -- TAB_GRPPO (Tabelle für Gruppen und Profile [n zu m] --
            CreateTableTAB_GRPPO();
        }

        private void InsertDefaultValuesTAB_GROUPConditions()
        {
            var sql = @"
                        INSERT INTO T1GROUP (GRNA, GTYP, CRAT, CHAT)
                        VALUES 
                        ('Completed', @gtyp, @crat, @chat),
                        ('Unplayed', @gtyp, @crat, @chat),
                        ('Currently Playing', @gtyp, @crat, @chat),
                        ('Playable', @gtyp, @crat, @chat);
                        ";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            command.Parameters.AddWithValue("@gtyp", GroupType.Condition);
            command.Parameters.AddWithValue("@crat", DateTime.Today);
            command.Parameters.AddWithValue("@chat", DateTime.Now);

            command.ExecuteNonQuery();
        }

        private void InsertDefaultValuesTAB_GROUPTags()
        {
            var sql = @"
                        INSERT INTO T1GROUP (GRNA, GTYP, CRAT, CHAT)
                        VALUES
                        ('Singleplayer', @gtyp, @crat, @chat),
                        ('Multiplayer', @gtyp, @crat, @chat),
                        ('Co-op', @gtyp, @crat, @chat),
                        ('PvP', @gtyp, @crat, @chat),

                        ('Action', @gtyp, @crat, @chat),
                        ('Adventure', @gtyp, @crat, @chat),
                        ('RPG', @gtyp, @crat, @chat),
                        ('Strategy', @gtyp, @crat, @chat),
                        ('Simulation', @gtyp, @crat, @chat),
                        ('Shooter', @gtyp, @crat, @chat),
                        ('Horror', @gtyp, @crat, @chat),
                        ('Survival', @gtyp, @crat, @chat),

                        ('Open World', @gtyp, @crat, @chat),
                        ('Sandbox', @gtyp, @crat, @chat),
                        ('Story Rich', @gtyp, @crat, @chat),
                        ('Exploration', @gtyp, @crat, @chat),
                        ('Crafting', @gtyp, @crat, @chat),
                        ('Building', @gtyp, @crat, @chat),

                        ('First Person', @gtyp, @crat, @chat),
                        ('Third Person', @gtyp, @crat, @chat),
                        ('Isometric', @gtyp, @crat, @chat),
                        ('Top-Down', @gtyp, @crat, @chat);
                    ";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            command.Parameters.AddWithValue("@gtyp", GroupType.Tag);
            command.Parameters.AddWithValue("@crat", DateTime.Today);
            command.Parameters.AddWithValue("@chat", DateTime.Now);

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Erstellt die Tabelle für die Beziehung zwischen TAB_GROUP und TAB_PROFI
        /// </summary>
        private void CreateTableTAB_GRPPO()
        {
            var sql = @"
            CREATE TABLE IF NOT EXISTS T1GRPPO (
                GPID INTEGER PRIMARY KEY AUTOINCREMENT,
                GRID INTEGER NOT NULL,
                PFID INTEGER NOT NULL,
                CRAT DATETIME,
                CHAT DATETIME,
                FOREIGN KEY (GRID) REFERENCES T1GROUP(GRID) ON DELETE CASCADE,
                FOREIGN KEY (PFID) REFERENCES T1PROFI(PFID) ON DELETE CASCADE
            );
        ";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Erstellt die Tabelle für die Groups (TAB_GROUP)
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void CreateTableTAB_GROUP()
        {
            var sql = @"
            CREATE TABLE IF NOT EXISTS T1GROUP (
                GRID INTEGER PRIMARY KEY AUTOINCREMENT,
                GRNA VARCHAR(200),
                GTYP VARCHAR(200),
                CRAT DATETIME,
                CHAT DATETIME
            );
        ";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Erstellt die Tabelle für die Sessions (TAB_SESSI)
        /// </summary>
        private void CreateTableTAB_SESSI()
        {
            var sql = @"
            CREATE TABLE IF NOT EXISTS T1SESSI (
                SEID INTEGER PRIMARY KEY AUTOINCREMENT,
                PFID INTEGER NOT NULL,
                PLFR DATETIME,
                PLTO DATETIME,
                PLTI REAL NOT NULL DEFAULT 0.0,
                CRAT DATETIME,
                CHAT DATETIME,
                FOREIGN KEY (PFID) REFERENCES T1PROFI(PFID) ON DELETE CASCADE
            );
        ";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Erstellt die Tabelle für die Profile (TAB_PROFI)
        /// </summary>
        private void CreateTableTAB_PROFI()
        {
            var sql = @"
                    CREATE TABLE IF NOT EXISTS T1PROFI (
                        PFID INTEGER PRIMARY KEY AUTOINCREMENT,
                        GANA VARCHAR(200),
                        FIPL DATETIME,
                        LAPL DATETIME,
                        PPFN VARCHAR(10000),
                        EXGF VARCHAR(10000),
                        SAID INTEGER,
                        PRSE TEXT,
                        EXEC TEXT,
                        PLSP DATETIME NOT NULL DEFAULT '0001-01-01 00:00:00',
                        CRAT DATETIME,
                        CHAT DATETIME,
                        ACCO VARCHAR(200),
                        ACIN VARCHAR(200),
                        ACAC INTEGER,
                        COMP INTEGER
                    );";

            if (_connection == null)
                return;

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }


    }
}
