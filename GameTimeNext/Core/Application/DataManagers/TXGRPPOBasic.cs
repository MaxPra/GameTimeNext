using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase.DevSync;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXGRPPOBasic
    {
        public virtual T1GRPPO CreateNew()
        {
            T1GRPPO obj = new T1GRPPO();
            obj.State = UIXTableObjectState.New;
            return obj;
        }

        public virtual void Save(T1GRPPO obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            if (Exists(connection, obj))
                Update(connection, obj);
            else
                Insert(connection, obj);

            obj.State = UIXTableObjectState.Available;
            obj.AcceptChanges();
            DevSyncCsvSyncService.ExportTableFor(obj);
        }

        public virtual void Delete(long gPID)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM T1GRPPO WHERE GPID = @GPID";
            cmd.Parameters.AddWithValue("@GPID", gPID); 
            cmd.ExecuteNonQuery();
            DevSyncCsvSyncService.ExportTable("T1GRPPO");
        }

        public virtual T1GRPPO? Read(long gPID)
        {
            UIXQuery query = new UIXQuery(K1GRPPO.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.GPID);
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.GRID);
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.PFID);
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.CRAT);
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.CHAT);
            query.AddWhere(K1GRPPO.Name, K1GRPPO.Fields.GPID, QueryCompareType.EQUALS, gPID);

            using var reader = query.Execute();
            if (!reader.Read())
                return null;

            T1GRPPO obj = Map(reader);
            obj.AcceptChanges();
            return obj;
        }

        public virtual List<T1GRPPO> ReadAll()
        {
            UIXQuery query = new UIXQuery(K1GRPPO.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.GPID);
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.GRID);
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.PFID);
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.CRAT);
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.CHAT);
            query.AddOrderBy(K1GRPPO.Name, K1GRPPO.Fields.GPID, OrderDirection.ASC);

            List<T1GRPPO> list = new List<T1GRPPO>();
            using var reader = query.Execute();
            while (reader.Read())

            {
                T1GRPPO obj = Map(reader);
                obj.AcceptChanges();
                list.Add(obj);
            }
            return list;
        }

        private void Insert(SQLiteConnection connection, T1GRPPO obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();
            DateTime now = DateTime.Now;
            obj.CRAT = now;
            obj.CHAT = now;
            cmd.CommandText = "INSERT INTO T1GRPPO (GRID, PFID, CRAT, CHAT) VALUES (@GRID, @PFID, @CRAT, @CHAT)";
            cmd.Parameters.AddWithValue("@GRID", ToDbValue(obj.GRID));
            cmd.Parameters.AddWithValue("@PFID", ToDbValue(obj.PFID));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.ExecuteNonQuery();
            using SQLiteCommand idCmd = connection.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid();";
            obj.GPID = Convert.ToInt64(idCmd.ExecuteScalar());
        }

        private void Update(SQLiteConnection connection, T1GRPPO obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();
            obj.CHAT = DateTime.Now;
            cmd.CommandText = "UPDATE T1GRPPO SET GRID = @GRID, PFID = @PFID, CRAT = @CRAT, CHAT = @CHAT WHERE GPID = @GPID";
            cmd.Parameters.AddWithValue("@GPID", ToDbValue(obj.GPID));
            cmd.Parameters.AddWithValue("@GRID", ToDbValue(obj.GRID));
            cmd.Parameters.AddWithValue("@PFID", ToDbValue(obj.PFID));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.ExecuteNonQuery();
        }

        private bool Exists(SQLiteConnection connection, T1GRPPO obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM T1GRPPO WHERE GPID = @GPID";
            cmd.Parameters.AddWithValue("@GPID", ToDbValue(obj.GPID));
            return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        protected static T1GRPPO Map(SQLiteDataReader reader)
        {
            T1GRPPO obj = new T1GRPPO();
            obj.GPID = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0));
            obj.GRID = reader.IsDBNull(1) ? 0 : Convert.ToInt64(reader.GetValue(1));
            obj.PFID = reader.IsDBNull(2) ? 0 : Convert.ToInt64(reader.GetValue(2));
            obj.CRAT = ParseDbDateTime(reader.GetValue(3));
            obj.CHAT = ParseDbDateTime(reader.GetValue(4));
            obj.State = UIXTableObjectState.Available;
            return obj;
        }

        private static object ToDbValue(object? value)
        {
            if (value is bool boolValue)
                return boolValue ? 1 : 0;
            if (value is DateTime dt)
                return dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            return value ?? DBNull.Value;
        }

        private static DateTime ParseDbDateTime(object? value)
        {
            if (value == null || value == DBNull.Value)
                return DateTime.MinValue;

            string raw = value.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
                return DateTime.MinValue;

            if (DateTime.TryParseExact(raw, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
                return parsed;
            if (DateTime.TryParseExact(raw, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                return parsed;
            if (DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsed))
                return parsed;
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                return parsed;

            return DateTime.MinValue;
        }

        protected static void EnsureOpen(SQLiteConnection connection)
        {
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();
        }
    }
}
