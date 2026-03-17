using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.DataManagers
{
    internal class TXSESSI
    {
        public T1SESSI CreateNew()
        {
            T1SESSI obj = new T1SESSI();

            DateTime now = DateTime.Now;
            obj.CRAT = now;
            obj.CHAT = now;

            obj.State = UIXTableObjectState.New;

            return obj;
        }

        public T1SESSI Copy(T1SESSI source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            T1SESSI copy = new T1SESSI();

            copy.SEID = 0;

            copy.PFID = source.PFID;
            copy.PTID = source.PTID;

            copy.PLFR = source.PLFR;
            copy.PLTO = source.PLTO;
            copy.PLTI = source.PLTI;

            DateTime now = DateTime.Now;
            copy.CRAT = now;
            copy.CHAT = now;

            copy.State = UIXTableObjectState.New;

            return copy;
        }

        public void Save(T1SESSI obj)
        {
            Save(obj, false);
        }

        public void Save(T1SESSI obj, bool migration)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            if (obj.PFID <= 0)
                throw new InvalidOperationException("PFID muss > 0 sein.");

            if (obj.PTID <= 0)
                throw new InvalidOperationException("PTID muss > 0 sein.");

            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            DateTime now = DateTime.Now;

            if (obj.SEID <= 0 || migration)
            {
                if (!migration)
                {
                    if (obj.CRAT == DateTime.MinValue)
                        obj.CRAT = now;

                    obj.CHAT = now;
                }

                Insert(connection, obj);
            }
            else
            {
                obj.CHAT = now;
                Update(connection, obj);
            }

            obj.State = UIXTableObjectState.Available;
            obj.AcceptChanges();
        }

        public void Delete(long seid)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText = "DELETE FROM T1SESSI WHERE SEID = @SEID;";
            cmd.Parameters.AddWithValue("@SEID", seid);

            cmd.ExecuteNonQuery();
        }

        public T1SESSI Read(long seid)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "SELECT SEID, PFID, PTID, PLFR, PLTO, PLTI, CRAT, CHAT " +
                "FROM T1SESSI WHERE SEID = @SEID;";

            cmd.Parameters.AddWithValue("@SEID", seid);

            using SQLiteDataReader reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            T1SESSI obj = Map(reader);
            obj.AcceptChanges();

            return obj;
        }

        public List<T1SESSI> ReadAll()
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            List<T1SESSI> list = new List<T1SESSI>();

            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "SELECT SEID, PFID, PTID, PLFR, PLTO, PLTI, CRAT, CHAT " +
                "FROM T1SESSI ORDER BY SEID;";

            using SQLiteDataReader reader = cmd.ExecuteReader();

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

            cmd.CommandText =
                "INSERT INTO T1SESSI (PFID, PTID, PLFR, PLTO, PLTI, CRAT, CHAT) " +
                "VALUES (@PFID, @PTID, @PLFR, @PLTO, @PLTI, @CRAT, @CHAT);";

            cmd.Parameters.AddWithValue("@PFID", obj.PFID);
            cmd.Parameters.AddWithValue("@PTID", obj.PTID);
            cmd.Parameters.AddWithValue("@PLFR", ToDbDateTime(obj.PLFR));
            cmd.Parameters.AddWithValue("@PLTO", ToDbDateTime(obj.PLTO));
            cmd.Parameters.AddWithValue("@PLTI", obj.PLTI);
            cmd.Parameters.AddWithValue("@CRAT", ToDbDateTime(obj.CRAT));
            cmd.Parameters.AddWithValue("@CHAT", ToDbDateTime(obj.CHAT));

            cmd.ExecuteNonQuery();

            using SQLiteCommand cmdId = connection.CreateCommand();

            cmdId.CommandText = "SELECT last_insert_rowid();";

            object result = cmdId.ExecuteScalar();

            obj.SEID = Convert.ToInt64(result);
        }

        private void Update(SQLiteConnection connection, T1SESSI obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "UPDATE T1SESSI SET " +
                "PFID = @PFID, " +
                "PTID = @PTID, " +
                "PLFR = @PLFR, " +
                "PLTO = @PLTO, " +
                "PLTI = @PLTI, " +
                "CRAT = @CRAT, " +
                "CHAT = @CHAT " +
                "WHERE SEID = @SEID;";

            cmd.Parameters.AddWithValue("@SEID", obj.SEID);
            cmd.Parameters.AddWithValue("@PFID", obj.PFID);
            cmd.Parameters.AddWithValue("@PTID", obj.PTID);
            cmd.Parameters.AddWithValue("@PLFR", ToDbDateTime(obj.PLFR));
            cmd.Parameters.AddWithValue("@PLTO", ToDbDateTime(obj.PLTO));
            cmd.Parameters.AddWithValue("@PLTI", obj.PLTI);
            cmd.Parameters.AddWithValue("@CRAT", ToDbDateTime(obj.CRAT));
            cmd.Parameters.AddWithValue("@CHAT", ToDbDateTime(obj.CHAT));

            cmd.ExecuteNonQuery();
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

        private T1SESSI Map(SQLiteDataReader reader)
        {
            T1SESSI obj = new T1SESSI();

            obj.SEID = Convert.ToInt64(reader.GetValue(0));
            obj.PFID = Convert.ToInt64(reader.GetValue(1));
            obj.PTID = Convert.ToInt64(reader.GetValue(2));

            obj.PLFR = ParseDbDateTime(reader.IsDBNull(3) ? null : reader.GetString(3));
            obj.PLTO = ParseDbDateTime(reader.IsDBNull(4) ? null : reader.GetString(4));

            obj.PLTI = reader.IsDBNull(5) ? 0.0 : Convert.ToDouble(reader.GetValue(5));

            obj.CRAT = ParseDbDateTime(reader.IsDBNull(6) ? null : reader.GetString(6));
            obj.CHAT = ParseDbDateTime(reader.IsDBNull(7) ? null : reader.GetString(7));

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
    }
}