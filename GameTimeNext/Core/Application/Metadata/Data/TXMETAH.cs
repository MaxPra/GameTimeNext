using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase.DevSync;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Metadata.Data
{
    public class TXMETAH
    {
        public T1METAH CreateNew()
        {
            T1METAH obj = new T1METAH();

            DateTime now = DateTime.Now;
            obj.CRAT = now;
            obj.CHAT = now;

            obj.State = UIXTableObjectState.New;

            return obj;
        }

        public T1METAH Copy(T1METAH source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            T1METAH copy = new T1METAH();

            copy.MENAM = source.MENAM;
            copy.DESCR = source.DESCR;
            copy.MTYPE = source.MTYPE;
            copy.DSYNC = source.DSYNC;
            copy.CRUS = FnOpSys.GetCurrentUserName();
            copy.CHUS = FnOpSys.GetCurrentUserName();

            DateTime now = DateTime.Now;
            copy.CRAT = now;
            copy.CHAT = now;

            copy.State = UIXTableObjectState.New;

            return copy;
        }

        public void Save(T1METAH obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            DateTime now = DateTime.Now;

            if (Exists(connection, obj.MENAM))
            {
                obj.CHAT = now;
                Update(connection, obj);
            }
            else
            {
                if (obj.CRAT == DateTime.MinValue)
                    obj.CRAT = now;

                obj.CHAT = now;

                Insert(connection, obj);
            }

            obj.State = UIXTableObjectState.Available;
            obj.AcceptChanges();

            DevSyncCsvSyncService.ExportTableFor(obj);
        }

        public void Delete(string menam)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "DELETE FROM T1METAH " +
                "WHERE MENAM = @MENAM;";

            cmd.Parameters.AddWithValue("@MENAM", menam);

            cmd.ExecuteNonQuery();

            DevSyncCsvSyncService.ExportTable("T1METAH");
        }

        public T1METAH Read(string menam)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "SELECT MENAM, DESCR, MTYPE, DSYNC, CRAT, CRUS, CHAT, CHUS " +
                "FROM T1METAH " +
                "WHERE MENAM = @MENAM;";

            cmd.Parameters.AddWithValue("@MENAM", menam);

            using SQLiteDataReader reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            T1METAH obj = Map(reader);
            obj.AcceptChanges();

            return obj;
        }

        public List<T1METAH> ReadAll()
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            List<T1METAH> list = new List<T1METAH>();

            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "SELECT MENAM, DESCR, MTYPE, DSYNC, CRAT, CRUS, CHAT, CHUS " +
                "FROM T1METAH " +
                "ORDER BY MENAM;";

            using SQLiteDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                T1METAH obj = Map(reader);
                obj.AcceptChanges();

                list.Add(obj);
            }

            return list;
        }

        private void Insert(SQLiteConnection connection, T1METAH obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "INSERT INTO T1METAH " +
                "(MENAM, DESCR, MTYPE, DSYNC, CRAT, CRUS, CHAT, CHUS) " +
                "VALUES " +
                "(@MENAM, @DESCR, @MTYPE, @DSYNC, @CRAT, @CRUS, @CHAT, @CHUS);";

            AddParameters(cmd, obj);

            cmd.ExecuteNonQuery();
        }

        private void Update(SQLiteConnection connection, T1METAH obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "UPDATE T1METAH SET " +
                "DESCR = @DESCR, " +
                "MTYPE = @MTYPE, " +
                "DSYNC = @DSYNC, " +
                "CRAT = @CRAT, " +
                "CRUS = @CRUS, " +
                "CHAT = @CHAT, " +
                "CHUS = @CHUS " +
                "WHERE MENAM = @MENAM;";

            AddParameters(cmd, obj);

            cmd.ExecuteNonQuery();
        }

        private bool Exists(SQLiteConnection connection, string menam)
        {
            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "SELECT COUNT(*) " +
                "FROM T1METAH " +
                "WHERE MENAM = @MENAM;";

            cmd.Parameters.AddWithValue("@MENAM", menam);

            return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        private void AddParameters(SQLiteCommand cmd, T1METAH obj)
        {
            cmd.Parameters.AddWithValue("@MENAM", obj.MENAM);
            cmd.Parameters.AddWithValue("@DESCR", obj.DESCR);
            cmd.Parameters.AddWithValue("@MTYPE", obj.MTYPE);
            cmd.Parameters.AddWithValue("@DSYNC", obj.DSYNC ? 1 : 0);
            cmd.Parameters.AddWithValue("@CRAT", ToDbDateTime(obj.CRAT));
            cmd.Parameters.AddWithValue("@CRUS", obj.CRUS);
            cmd.Parameters.AddWithValue("@CHAT", ToDbDateTime(obj.CHAT));
            cmd.Parameters.AddWithValue("@CHUS", obj.CHUS);
        }

        private T1METAH Map(SQLiteDataReader reader)
        {
            T1METAH obj = new T1METAH();

            obj.MENAM = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            obj.DESCR = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            obj.MTYPE = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            obj.DSYNC = !reader.IsDBNull(3) && Convert.ToInt32(reader.GetValue(3)) == 1;

            obj.CRAT = ParseDbDateTime(reader.IsDBNull(4) ? null : reader.GetString(4));
            obj.CRUS = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);
            obj.CHAT = ParseDbDateTime(reader.IsDBNull(6) ? null : reader.GetString(6));
            obj.CHUS = reader.IsDBNull(7) ? string.Empty : reader.GetString(7);

            obj.State = UIXTableObjectState.Available;

            return obj;
        }

        private DateTime ParseDbDateTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return DateTime.MinValue;

            if (DateTime.TryParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                return result;

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return result;

            return DateTime.MinValue;
        }

        private string ToDbDateTime(DateTime value)
        {
            return value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private void EnsureOpen(SQLiteConnection connection)
        {
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();
        }
    }
}
