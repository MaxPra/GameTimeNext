using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase.DevSync;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXCTABDBasic
    {
        public virtual T1CTABD CreateNew()
        {
            T1CTABD obj = new T1CTABD();
            obj.State = UIXTableObjectState.New;
            return obj;
        }

        public virtual void Save(T1CTABD obj)
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

        public virtual void Delete(string tXTYP, string tXNUM)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM T1CTABD WHERE TXTYP = @TXTYP AND TXNUM = @TXNUM";
            cmd.Parameters.AddWithValue("@TXTYP", tXTYP);
            cmd.Parameters.AddWithValue("@TXNUM", tXNUM);
            cmd.ExecuteNonQuery();
            DevSyncCsvSyncService.ExportTable("T1CTABD");
        }

        public virtual T1CTABD? Read(string tXTYP, string tXNUM)
        {
            UIXQuery query = new UIXQuery(K1CTABD.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddField(K1CTABD.Name, K1CTABD.Fields.TXTYP);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.TXNUM);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.DESCR);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.CRAT);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.CHAT);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.PARM1);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.PARM2);
            query.AddWhere(K1CTABD.Name, K1CTABD.Fields.TXTYP, QueryCompareType.EQUALS, tXTYP);
            query.AddWhere(K1CTABD.Name, K1CTABD.Fields.TXNUM, QueryCompareType.EQUALS, tXNUM);

            using var reader = query.Execute();
            if (!reader.Read())
                return null;

            T1CTABD obj = Map(reader);
            obj.AcceptChanges();
            return obj;
        }

        public virtual List<T1CTABD> ReadAll()
        {
            UIXQuery query = new UIXQuery(K1CTABD.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddField(K1CTABD.Name, K1CTABD.Fields.TXTYP);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.TXNUM);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.DESCR);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.CRAT);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.CHAT);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.PARM1);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.PARM2);
            query.AddOrderBy(K1CTABD.Name, K1CTABD.Fields.TXTYP, OrderDirection.ASC);

            List<T1CTABD> list = new List<T1CTABD>();
            using var reader = query.Execute();
            while (reader.Read())

            {
                T1CTABD obj = Map(reader);
                obj.AcceptChanges();
                list.Add(obj);
            }
            return list;
        }

        private void Insert(SQLiteConnection connection, T1CTABD obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO T1CTABD (TXTYP, TXNUM, DESCR, CRAT, CHAT, PARM1, PARM2) VALUES (@TXTYP, @TXNUM, @DESCR, @CRAT, @CHAT, @PARM1, @PARM2)";
            cmd.Parameters.AddWithValue("@TXTYP", ToDbValue(obj.TXTYP));
            cmd.Parameters.AddWithValue("@TXNUM", ToDbValue(obj.TXNUM));
            cmd.Parameters.AddWithValue("@DESCR", ToDbValue(obj.DESCR));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@PARM1", ToDbValue(obj.PARM1));
            cmd.Parameters.AddWithValue("@PARM2", ToDbValue(obj.PARM2));
            cmd.ExecuteNonQuery();
        }

        private void Update(SQLiteConnection connection, T1CTABD obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE T1CTABD SET DESCR = @DESCR, CRAT = @CRAT, CHAT = @CHAT, PARM1 = @PARM1, PARM2 = @PARM2 WHERE TXTYP = @TXTYP AND TXNUM = @TXNUM";
            cmd.Parameters.AddWithValue("@TXTYP", ToDbValue(obj.TXTYP));
            cmd.Parameters.AddWithValue("@TXNUM", ToDbValue(obj.TXNUM));
            cmd.Parameters.AddWithValue("@DESCR", ToDbValue(obj.DESCR));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@PARM1", ToDbValue(obj.PARM1));
            cmd.Parameters.AddWithValue("@PARM2", ToDbValue(obj.PARM2));
            cmd.ExecuteNonQuery();
        }

        private bool Exists(SQLiteConnection connection, T1CTABD obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM T1CTABD WHERE TXTYP = @TXTYP AND TXNUM = @TXNUM";
            cmd.Parameters.AddWithValue("@TXTYP", ToDbValue(obj.TXTYP));
            cmd.Parameters.AddWithValue("@TXNUM", ToDbValue(obj.TXNUM));
            return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        protected static T1CTABD Map(SQLiteDataReader reader)
        {
            T1CTABD obj = new T1CTABD();
            obj.TXTYP = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            obj.TXNUM = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            obj.DESCR = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            obj.CRAT = ParseDbDateTime(reader.GetValue(3));
            obj.CHAT = ParseDbDateTime(reader.GetValue(4));
            obj.PARM1 = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);
            obj.PARM2 = reader.IsDBNull(6) ? string.Empty : reader.GetString(6);
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
