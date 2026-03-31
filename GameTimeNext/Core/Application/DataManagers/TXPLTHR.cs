using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXPLTHR
    {
        public T1PLTHR CreateNew()
        {
            T1PLTHR obj = new T1PLTHR();

            DateTime now = DateTime.Now;
            obj.CRAT = now;
            obj.CHAT = now;

            obj.State = UIXTableObjectState.New;

            return obj;
        }

        public T1PLTHR Copy(T1PLTHR source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            T1PLTHR copy = new T1PLTHR();

            copy.PTID = 0;
            copy.PFID = source.PFID;

            copy.PTTY = source.PTTY;
            copy.PTDE = source.PTDE;
            copy.PTCO = source.PTCO;
            copy.PTCA = source.PTCA;

            DateTime now = DateTime.Now;
            copy.CRAT = now;
            copy.CHAT = now;

            copy.State = UIXTableObjectState.New;

            return copy;
        }

        public void Save(T1PLTHR obj)
        {
            Save(obj, false);
        }

        public void Save(T1PLTHR obj, bool migration)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            if (obj.PFID <= 0)
                throw new InvalidOperationException("PFID muss > 0 sein.");

            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            DateTime now = DateTime.Now;

            if (obj.PTID <= 0 || migration)
            {
                if (!migration)
                {
                    if (obj.CRAT == DateTime.MinValue) obj.CRAT = now;
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

        public void Delete(long ptid)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM T1PLTHR WHERE PTID = @PTID;";
                cmd.Parameters.AddWithValue("@PTID", ptid);
                cmd.ExecuteNonQuery();
            }
        }

        public T1PLTHR Read(long ptid)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT PTID, PFID, PTTY, PTDE, PTCO, PTCA, CRAT, CHAT " +
                    "FROM T1PLTHR WHERE PTID = @PTID;";
                cmd.Parameters.AddWithValue("@PTID", ptid);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    T1PLTHR obj = Map(reader);
                    obj.AcceptChanges();
                    return obj;
                }
            }
        }

        public List<T1PLTHR> ReadAll()
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            List<T1PLTHR> list = new List<T1PLTHR>();

            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT PTID, PFID, PTTY, PTDE, PTCO, PTCA, CRAT, CHAT " +
                    "FROM T1PLTHR ORDER BY PTID;";

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        T1PLTHR obj = Map(reader);
                        obj.AcceptChanges();
                        list.Add(obj);
                    }
                }
            }

            return list;
        }

        public List<T1PLTHR> ReadAllByProfile(long pfid)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            List<T1PLTHR> list = new List<T1PLTHR>();

            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT PTID, PFID, PTTY, PTDE, PTCO, PTCA, CRAT, CHAT " +
                    "FROM T1PLTHR WHERE PFID = @PFID ORDER BY PTID;";

                cmd.Parameters.AddWithValue("@PFID", pfid);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        T1PLTHR obj = Map(reader);
                        obj.AcceptChanges();
                        list.Add(obj);
                    }
                }
            }

            return list;
        }

        private void Insert(SQLiteConnection connection, T1PLTHR obj)
        {
            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "INSERT INTO T1PLTHR (PFID, PTTY, PTDE, PTCO, PTCA, CRAT, CHAT) " +
                    "VALUES (@PFID, @PTTY, @PTDE, @PTCO, @PTCA, @CRAT, @CHAT);";

                cmd.Parameters.AddWithValue("@PFID", obj.PFID);
                cmd.Parameters.AddWithValue("@PTTY", obj.PTTY ?? string.Empty);
                cmd.Parameters.AddWithValue("@PTDE", obj.PTDE ?? string.Empty);
                cmd.Parameters.AddWithValue("@PTCO", obj.PTCO ? 1 : 0);
                cmd.Parameters.AddWithValue("@PTCA", obj.PTCA ? 1 : 0);
                cmd.Parameters.AddWithValue("@CRAT", ToDbDateTime(obj.CRAT));
                cmd.Parameters.AddWithValue("@CHAT", ToDbDateTime(obj.CHAT));

                cmd.ExecuteNonQuery();
            }

            using (SQLiteCommand cmdId = connection.CreateCommand())
            {
                cmdId.CommandText = "SELECT last_insert_rowid();";
                object result = cmdId.ExecuteScalar();
                obj.PTID = Convert.ToInt64(result);
            }
        }

        private void Update(SQLiteConnection connection, T1PLTHR obj)
        {
            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "UPDATE T1PLTHR SET " +
                    "PFID = @PFID, " +
                    "PTTY = @PTTY, " +
                    "PTDE = @PTDE, " +
                    "PTCO = @PTCO, " +
                    "PTCA = @PTCA, " +
                    "CRAT = @CRAT, " +
                    "CHAT = @CHAT " +
                    "WHERE PTID = @PTID;";

                cmd.Parameters.AddWithValue("@PTID", obj.PTID);
                cmd.Parameters.AddWithValue("@PFID", obj.PFID);
                cmd.Parameters.AddWithValue("@PTTY", obj.PTTY ?? string.Empty);
                cmd.Parameters.AddWithValue("@PTDE", obj.PTDE ?? string.Empty);
                cmd.Parameters.AddWithValue("@PTCO", obj.PTCO ? 1 : 0);
                cmd.Parameters.AddWithValue("@PTCA", obj.PTCA ? 1 : 0);
                cmd.Parameters.AddWithValue("@CRAT", ToDbDateTime(obj.CRAT));
                cmd.Parameters.AddWithValue("@CHAT", ToDbDateTime(obj.CHAT));

                cmd.ExecuteNonQuery();
            }
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

        private T1PLTHR Map(SQLiteDataReader reader)
        {
            T1PLTHR obj = new T1PLTHR();

            obj.PTID = Convert.ToInt64(reader.GetValue(0));
            obj.PFID = reader.IsDBNull(1) ? 0 : Convert.ToInt64(reader.GetValue(1));
            obj.PTTY = reader.IsDBNull(2) ? string.Empty : Convert.ToString(reader.GetValue(2)) ?? string.Empty;
            obj.PTDE = reader.IsDBNull(3) ? string.Empty : Convert.ToString(reader.GetValue(3)) ?? string.Empty;
            obj.PTCO = !reader.IsDBNull(4) && Convert.ToInt64(reader.GetValue(4)) == 1;
            obj.PTCA = !reader.IsDBNull(5) && Convert.ToInt64(reader.GetValue(5)) == 1;
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