using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase.DevSync;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXCTABD
    {
        public T1CTABD CreateNew()
        {
            T1CTABD obj = new T1CTABD();

            DateTime now = DateTime.Now;
            obj.CRAT = now;
            obj.CHAT = now;

            obj.State = UIXTableObjectState.New;

            return obj;
        }

        public T1CTABD Copy(T1CTABD source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            T1CTABD copy = new T1CTABD();

            copy.TXTYP = source.TXTYP;
            copy.TXNUM = source.TXNUM;
            copy.DESCR = source.DESCR;
            copy.PARM1 = source.PARM1;
            copy.PARM2 = source.PARM2;

            DateTime now = DateTime.Now;
            copy.CRAT = now;
            copy.CHAT = now;

            copy.State = UIXTableObjectState.New;

            return copy;
        }

        public void Save(T1CTABD obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            DateTime now = DateTime.Now;

            if (Exists(connection, obj.TXTYP, obj.TXNUM))
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

        public void Delete(string txtyp, string txnum)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "DELETE FROM T1CTABD " +
                "WHERE TXTYP = @TXTYP " +
                "AND TXNUM = @TXNUM;";

            cmd.Parameters.AddWithValue("@TXTYP", txtyp);
            cmd.Parameters.AddWithValue("@TXNUM", txnum);

            cmd.ExecuteNonQuery();
        }

        public void DeleteAllEntries(string txtyp)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "DELETE FROM T1CTABD " +
                "WHERE TXTYP = @TXTYP;";

            cmd.Parameters.AddWithValue("@TXTYP", txtyp);

            cmd.ExecuteNonQuery();
        }

        public T1CTABD Read(string txtyp, string txnum)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "SELECT TXTYP, TXNUM, DESCR, PARM1, PARM2, CRAT, CHAT " +
                "FROM T1CTABD " +
                "WHERE TXTYP = @TXTYP " +
                "AND TXNUM = @TXNUM;";

            cmd.Parameters.AddWithValue("@TXTYP", txtyp);
            cmd.Parameters.AddWithValue("@TXNUM", txnum);

            using SQLiteDataReader reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            T1CTABD obj = Map(reader);
            obj.AcceptChanges();

            return obj;
        }

        public List<T1CTABD> ReadAll()
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            List<T1CTABD> list = new List<T1CTABD>();

            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "SELECT TXTYP, TXNUM, DESCR, PARM1, PARM2, CRAT, CHAT " +
                "FROM T1CTABD " +
                "ORDER BY TXTYP, TXNUM;";

            using SQLiteDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                T1CTABD obj = Map(reader);
                obj.AcceptChanges();

                list.Add(obj);
            }

            return list;
        }

        public List<T1CTABD> GetEntries(string txtyp)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            List<T1CTABD> list = new List<T1CTABD>();

            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "SELECT TXTYP, TXNUM, DESCR, PARM1, PARM2, CRAT, CHAT " +
                "FROM T1CTABD " +
                "WHERE TXTYP = @TXTYP " +
                "ORDER BY TXNUM;";

            cmd.Parameters.AddWithValue("@TXTYP", txtyp);

            using SQLiteDataReader reader = cmd.ExecuteReader();

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

            cmd.CommandText =
                "INSERT INTO T1CTABD " +
                "(TXTYP, TXNUM, DESCR, PARM1, PARM2, CRAT, CHAT) " +
                "VALUES " +
                "(@TXTYP, @TXNUM, @DESCR, @PARM1, @PARM2, @CRAT, @CHAT);";

            cmd.Parameters.AddWithValue("@TXTYP", obj.TXTYP);
            cmd.Parameters.AddWithValue("@TXNUM", obj.TXNUM);
            cmd.Parameters.AddWithValue("@DESCR", obj.DESCR);
            cmd.Parameters.AddWithValue("@PARM1", obj.PARM1);
            cmd.Parameters.AddWithValue("@PARM2", obj.PARM2);
            cmd.Parameters.AddWithValue("@CRAT", ToDbDateTime(obj.CRAT));
            cmd.Parameters.AddWithValue("@CHAT", ToDbDateTime(obj.CHAT));

            cmd.ExecuteNonQuery();
        }

        private void Update(SQLiteConnection connection, T1CTABD obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "UPDATE T1CTABD SET " +
                "DESCR = @DESCR, " +
                "PARM1 = @PARM1, " +
                "PARM2 = @PARM2, " +
                "CHAT = @CHAT " +
                "WHERE TXTYP = @TXTYP " +
                "AND TXNUM = @TXNUM;";

            cmd.Parameters.AddWithValue("@TXTYP", obj.TXTYP);
            cmd.Parameters.AddWithValue("@TXNUM", obj.TXNUM);
            cmd.Parameters.AddWithValue("@DESCR", obj.DESCR);
            cmd.Parameters.AddWithValue("@PARM1", obj.PARM1);
            cmd.Parameters.AddWithValue("@PARM2", obj.PARM2);
            cmd.Parameters.AddWithValue("@CHAT", ToDbDateTime(obj.CHAT));

            cmd.ExecuteNonQuery();
        }

        private bool Exists(SQLiteConnection connection, string txtyp, string txnum)
        {
            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "SELECT COUNT(*) " +
                "FROM T1CTABD " +
                "WHERE TXTYP = @TXTYP " +
                "AND TXNUM = @TXNUM;";

            cmd.Parameters.AddWithValue("@TXTYP", txtyp);
            cmd.Parameters.AddWithValue("@TXNUM", txnum);

            return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        private T1CTABD Map(SQLiteDataReader reader)
        {
            T1CTABD obj = new T1CTABD();

            obj.TXTYP = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            obj.TXNUM = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            obj.DESCR = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            obj.PARM1 = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
            obj.PARM2 = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);

            obj.CRAT = ParseDbDateTime(reader.IsDBNull(5) ? null : reader.GetString(5));
            obj.CHAT = ParseDbDateTime(reader.IsDBNull(6) ? null : reader.GetString(6));

            obj.State = UIXTableObjectState.Available;

            return obj;
        }

        private void EnsureOpen(SQLiteConnection connection)
        {
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();
        }

        private string ToDbDateTime(DateTime value)
        {
            return value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private DateTime ParseDbDateTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return DateTime.MinValue;

            if (DateTime.TryParseExact(
                value,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime result))
            {
                return result;
            }

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return result;

            return DateTime.MinValue;
        }
    }
}