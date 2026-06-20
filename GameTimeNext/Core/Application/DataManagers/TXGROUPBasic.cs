using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase.DevSync;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXGROUPBasic
    {
        public virtual T1GROUP CreateNew()
        {
            T1GROUP obj = new T1GROUP();
            obj.State = UIXTableObjectState.New;
            return obj;
        }

        public virtual void Save(T1GROUP obj)
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

        public virtual void Delete(long gRID)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM T1GROUP WHERE GRID = @GRID";
            cmd.Parameters.AddWithValue("@GRID", gRID); 
            cmd.ExecuteNonQuery();
            DevSyncCsvSyncService.ExportTable("T1GROUP");
        }

        public virtual T1GROUP? Read(long gRID)
        {
            UIXQuery query = new UIXQuery(K1GROUP.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddField(K1GROUP.Name, K1GROUP.Fields.GRID);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.GRNA);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.GTYP);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.CRAT);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.CHAT);
            query.AddWhere(K1GROUP.Name, K1GROUP.Fields.GRID, QueryCompareType.EQUALS, gRID);

            using var reader = query.Execute();
            if (!reader.Read())
                return null;

            T1GROUP obj = Map(reader);
            obj.AcceptChanges();
            return obj;
        }

        public virtual List<T1GROUP> ReadAll()
        {
            UIXQuery query = new UIXQuery(K1GROUP.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddField(K1GROUP.Name, K1GROUP.Fields.GRID);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.GRNA);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.GTYP);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.CRAT);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.CHAT);
            query.AddOrderBy(K1GROUP.Name, K1GROUP.Fields.GRID, OrderDirection.ASC);

            List<T1GROUP> list = new List<T1GROUP>();
            using var reader = query.Execute();
            while (reader.Read())

            {
                T1GROUP obj = Map(reader);
                obj.AcceptChanges();
                list.Add(obj);
            }
            return list;
        }

        private void Insert(SQLiteConnection connection, T1GROUP obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO T1GROUP (GRID, GRNA, GTYP, CRAT, CHAT) VALUES (@GRID, @GRNA, @GTYP, @CRAT, @CHAT)";
            cmd.Parameters.AddWithValue("@GRID", ToDbValue(obj.GRID));
            cmd.Parameters.AddWithValue("@GRNA", ToDbValue(obj.GRNA));
            cmd.Parameters.AddWithValue("@GTYP", ToDbValue(obj.GTYP));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.ExecuteNonQuery();
        }

        private void Update(SQLiteConnection connection, T1GROUP obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE T1GROUP SET GRNA = @GRNA, GTYP = @GTYP, CRAT = @CRAT, CHAT = @CHAT WHERE GRID = @GRID";
            cmd.Parameters.AddWithValue("@GRID", ToDbValue(obj.GRID));
            cmd.Parameters.AddWithValue("@GRNA", ToDbValue(obj.GRNA));
            cmd.Parameters.AddWithValue("@GTYP", ToDbValue(obj.GTYP));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.ExecuteNonQuery();
        }

        private bool Exists(SQLiteConnection connection, T1GROUP obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM T1GROUP WHERE GRID = @GRID";
            cmd.Parameters.AddWithValue("@GRID", ToDbValue(obj.GRID));
            return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        protected static T1GROUP Map(SQLiteDataReader reader)
        {
            T1GROUP obj = new T1GROUP();
            obj.GRID = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0));
            obj.GRNA = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            obj.GTYP = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
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
