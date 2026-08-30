using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase.Migration;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXSESSIBasic
    {
        public virtual T1SESSI CreateNew()
        {
            T1SESSI obj = new T1SESSI();
            obj.State = UIXTableObjectState.New;
            return obj;
        }

        public virtual void Save(T1SESSI obj)
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

        public virtual void Delete(long sEID)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM T1SESSI WHERE SEID = @SEID";
            cmd.Parameters.AddWithValue("@SEID", sEID); 
            cmd.ExecuteNonQuery();
            MigrationFactory.ExportCsvFile("T1SESSI");
        }

        public virtual T1SESSI? Read(long sEID)
        {
            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddField(K1SESSI.Name, K1SESSI.Fields.SEID);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PFID);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PTID);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PLFR);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PLTO);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PLTI);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.CRAT);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.CHAT);
            query.AddWhere(K1SESSI.Name, K1SESSI.Fields.SEID, QueryCompareType.EQUALS, sEID);

            using var reader = query.Execute();
            if (!reader.Read())
                return null;

            T1SESSI obj = Map(reader);
            obj.AcceptChanges();
            return obj;
        }

        public virtual List<T1SESSI> ReadAll()
        {
            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddField(K1SESSI.Name, K1SESSI.Fields.SEID);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PFID);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PTID);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PLFR);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PLTO);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PLTI);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.CRAT);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.CHAT);
            query.AddOrderBy(K1SESSI.Name, K1SESSI.Fields.SEID, OrderDirection.ASC);

            List<T1SESSI> list = new List<T1SESSI>();
            using var reader = query.Execute();
            while (reader.Read())

            {
                T1SESSI obj = Map(reader);
                obj.AcceptChanges();
                list.Add(obj);
            }
            return list;
        }

        private void Insert(SQLiteConnection connection, T1SESSI obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();
            DateTime now = DateTime.Now;
            obj.CRAT = now;
            obj.CHAT = now;
            cmd.CommandText = "INSERT INTO T1SESSI (PFID, PTID, PLFR, PLTO, PLTI, CRAT, CHAT) VALUES (@PFID, @PTID, @PLFR, @PLTO, @PLTI, @CRAT, @CHAT)";
            cmd.Parameters.AddWithValue("@PFID", ToDbValue(obj.PFID));
            cmd.Parameters.AddWithValue("@PTID", ToDbValue(obj.PTID));
            cmd.Parameters.AddWithValue("@PLFR", ToDbValue(obj.PLFR));
            cmd.Parameters.AddWithValue("@PLTO", ToDbValue(obj.PLTO));
            cmd.Parameters.AddWithValue("@PLTI", ToDbValue(obj.PLTI));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.ExecuteNonQuery();
            using SQLiteCommand idCmd = connection.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid();";
            obj.SEID = Convert.ToInt64(idCmd.ExecuteScalar());
        }

        private void Update(SQLiteConnection connection, T1SESSI obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();
            obj.CHAT = DateTime.Now;
            cmd.CommandText = "UPDATE T1SESSI SET PFID = @PFID, PTID = @PTID, PLFR = @PLFR, PLTO = @PLTO, PLTI = @PLTI, CRAT = @CRAT, CHAT = @CHAT WHERE SEID = @SEID";
            cmd.Parameters.AddWithValue("@SEID", ToDbValue(obj.SEID));
            cmd.Parameters.AddWithValue("@PFID", ToDbValue(obj.PFID));
            cmd.Parameters.AddWithValue("@PTID", ToDbValue(obj.PTID));
            cmd.Parameters.AddWithValue("@PLFR", ToDbValue(obj.PLFR));
            cmd.Parameters.AddWithValue("@PLTO", ToDbValue(obj.PLTO));
            cmd.Parameters.AddWithValue("@PLTI", ToDbValue(obj.PLTI));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.ExecuteNonQuery();
        }

        private bool Exists(SQLiteConnection connection, T1SESSI obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM T1SESSI WHERE SEID = @SEID";
            cmd.Parameters.AddWithValue("@SEID", ToDbValue(obj.SEID));
            return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        protected static T1SESSI Map(SQLiteDataReader reader)
        {
            T1SESSI obj = new T1SESSI();
            obj.SEID = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0));
            obj.PFID = reader.IsDBNull(1) ? 0 : Convert.ToInt64(reader.GetValue(1));
            obj.PTID = reader.IsDBNull(2) ? 0 : Convert.ToInt64(reader.GetValue(2));
            obj.PLFR = ParseDbDateTime(reader.GetValue(3));
            obj.PLTO = ParseDbDateTime(reader.GetValue(4));
            obj.PLTI = reader.IsDBNull(5) ? 0d : Convert.ToDouble(reader.GetValue(5), CultureInfo.InvariantCulture);
            obj.CRAT = ParseDbDateTime(reader.GetValue(6));
            obj.CHAT = ParseDbDateTime(reader.GetValue(7));
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
