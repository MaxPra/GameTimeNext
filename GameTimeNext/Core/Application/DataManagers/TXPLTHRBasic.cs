using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase.Migration;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXPLTHRBasic
    {
        public virtual T1PLTHR CreateNew()
        {
            T1PLTHR obj = new T1PLTHR();
            obj.State = UIXTableObjectState.New;
            return obj;
        }

        public virtual void Save(T1PLTHR obj)
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
            MigrationFactory.ExportCsvFileFor(obj);
        }

        public virtual void Delete(long pTID)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM T1PLTHR WHERE PTID = @PTID";
            cmd.Parameters.AddWithValue("@PTID", pTID); 
            cmd.ExecuteNonQuery();
            MigrationFactory.ExportCsvFile("T1PLTHR");
        }

        public virtual T1PLTHR? Read(long pTID)
        {
            UIXQuery query = new UIXQuery(K1PLTHR.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTID);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PFID);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTTY);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTDE);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTCO);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.CRAT);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.CHAT);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTCA);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTPA);
            query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PTID, QueryCompareType.EQUALS, pTID);

            using var reader = query.Execute();
            if (!reader.Read())
                return null;

            T1PLTHR obj = Map(reader);
            obj.AcceptChanges();
            return obj;
        }

        public virtual List<T1PLTHR> ReadAll()
        {
            UIXQuery query = new UIXQuery(K1PLTHR.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTID);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PFID);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTTY);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTDE);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTCO);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.CRAT);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.CHAT);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTCA);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTPA);
            query.AddOrderBy(K1PLTHR.Name, K1PLTHR.Fields.PTID, OrderDirection.ASC);

            List<T1PLTHR> list = new List<T1PLTHR>();
            using var reader = query.Execute();
            while (reader.Read())

            {
                T1PLTHR obj = Map(reader);
                obj.AcceptChanges();
                list.Add(obj);
            }
            return list;
        }

        private void Insert(SQLiteConnection connection, T1PLTHR obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();
            DateTime now = DateTime.Now;
            obj.CRAT = now;
            obj.CHAT = now;
            cmd.CommandText = "INSERT INTO T1PLTHR (PFID, PTTY, PTDE, PTCO, CRAT, CHAT, PTCA, PTPA) VALUES (@PFID, @PTTY, @PTDE, @PTCO, @CRAT, @CHAT, @PTCA, @PTPA)";
            cmd.Parameters.AddWithValue("@PFID", ToDbValue(obj.PFID));
            cmd.Parameters.AddWithValue("@PTTY", ToDbValue(obj.PTTY));
            cmd.Parameters.AddWithValue("@PTDE", ToDbValue(obj.PTDE));
            cmd.Parameters.AddWithValue("@PTCO", ToDbValue(obj.PTCO));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@PTCA", ToDbValue(obj.PTCA));
            cmd.Parameters.AddWithValue("@PTPA", ToDbValue(obj.PTPA));
            cmd.ExecuteNonQuery();
            using SQLiteCommand idCmd = connection.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid();";
            obj.PTID = Convert.ToInt64(idCmd.ExecuteScalar());
        }

        private void Update(SQLiteConnection connection, T1PLTHR obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();
            obj.CHAT = DateTime.Now;
            cmd.CommandText = "UPDATE T1PLTHR SET PFID = @PFID, PTTY = @PTTY, PTDE = @PTDE, PTCO = @PTCO, CRAT = @CRAT, CHAT = @CHAT, PTCA = @PTCA, PTPA = @PTPA WHERE PTID = @PTID";
            cmd.Parameters.AddWithValue("@PTID", ToDbValue(obj.PTID));
            cmd.Parameters.AddWithValue("@PFID", ToDbValue(obj.PFID));
            cmd.Parameters.AddWithValue("@PTTY", ToDbValue(obj.PTTY));
            cmd.Parameters.AddWithValue("@PTDE", ToDbValue(obj.PTDE));
            cmd.Parameters.AddWithValue("@PTCO", ToDbValue(obj.PTCO));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@PTCA", ToDbValue(obj.PTCA));
            cmd.Parameters.AddWithValue("@PTPA", ToDbValue(obj.PTPA));
            cmd.ExecuteNonQuery();
        }

        private bool Exists(SQLiteConnection connection, T1PLTHR obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM T1PLTHR WHERE PTID = @PTID";
            cmd.Parameters.AddWithValue("@PTID", ToDbValue(obj.PTID));
            return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        protected static T1PLTHR Map(SQLiteDataReader reader)
        {
            T1PLTHR obj = new T1PLTHR();
            obj.PTID = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0));
            obj.PFID = reader.IsDBNull(1) ? 0 : Convert.ToInt64(reader.GetValue(1));
            obj.PTTY = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            obj.PTDE = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
            obj.PTCO = !reader.IsDBNull(4) && Convert.ToInt32(reader.GetValue(4)) == 1;
            obj.CRAT = ParseDbDateTime(reader.GetValue(5));
            obj.CHAT = ParseDbDateTime(reader.GetValue(6));
            obj.PTCA = !reader.IsDBNull(7) && Convert.ToInt32(reader.GetValue(7)) == 1;
            obj.PTPA = !reader.IsDBNull(8) && Convert.ToInt32(reader.GetValue(8)) == 1;
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
