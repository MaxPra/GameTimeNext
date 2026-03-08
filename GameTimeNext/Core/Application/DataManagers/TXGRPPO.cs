using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.DataManagers
{
    internal class TXGRPPO
    {
        public T1GRPPO CreateNew()
        {
            T1GRPPO obj = new T1GRPPO();

            DateTime now = DateTime.Now;
            obj.CRAT = now;
            obj.CHAT = now;

            obj.State = UIXTableObjectState.New;

            return obj;
        }

        public T1GRPPO Copy(T1GRPPO source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            T1GRPPO copy = new T1GRPPO();

            copy.GPID = 0;

            copy.GRID = source.GRID;
            copy.PFID = source.PFID;

            DateTime now = DateTime.Now;
            copy.CRAT = now;
            copy.CHAT = now;

            copy.State = UIXTableObjectState.New;

            return copy;
        }

        public void Save(T1GRPPO obj)
        {
            Save(obj, false);
        }

        public void Save(T1GRPPO obj, bool migration)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            if (obj.GRID <= 0)
                throw new InvalidOperationException("GRID muss > 0 sein, da T1GRPPO auf eine Gruppe referenziert.");

            if (obj.PFID <= 0)
                throw new InvalidOperationException("PFID muss > 0 sein, da T1GRPPO auf ein Profil referenziert.");

            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            DateTime now = DateTime.Now;

            if (obj.GPID <= 0 || migration)
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

        public void Delete(long gpid)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM T1GRPPO WHERE GPID = @GPID;";
                cmd.Parameters.AddWithValue("@GPID", gpid);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteAllWherePFID(long pfid)
        {
            if (pfid == 0)
                return;

            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM T1GRPPO WHERE PFID = @PFID;";
                cmd.Parameters.AddWithValue("@PFID", pfid);
                cmd.ExecuteNonQuery();
            }
        }

        public T1GRPPO Read(long gpid)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT GPID, GRID, PFID, CRAT, CHAT FROM T1GRPPO WHERE GPID = @GPID;";
                cmd.Parameters.AddWithValue("@GPID", gpid);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    T1GRPPO obj = Map(reader);
                    obj.AcceptChanges();
                    return obj;
                }
            }
        }

        public List<T1GRPPO> ReadAll()
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            List<T1GRPPO> list = new List<T1GRPPO>();

            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT GPID, GRID, PFID, CRAT, CHAT FROM T1GRPPO ORDER BY GPID;";

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        T1GRPPO obj = Map(reader);
                        obj.AcceptChanges();
                        list.Add(obj);
                    }
                }
            }

            return list;
        }

        private void Insert(SQLiteConnection connection, T1GRPPO obj)
        {
            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "INSERT INTO T1GRPPO (GRID, PFID, CRAT, CHAT) " +
                    "VALUES (@GRID, @PFID, @CRAT, @CHAT);";

                cmd.Parameters.AddWithValue("@GRID", obj.GRID);
                cmd.Parameters.AddWithValue("@PFID", obj.PFID);
                cmd.Parameters.AddWithValue("@CRAT", ToDbDateTime(obj.CRAT));
                cmd.Parameters.AddWithValue("@CHAT", ToDbDateTime(obj.CHAT));

                cmd.ExecuteNonQuery();
            }

            using (SQLiteCommand cmdId = connection.CreateCommand())
            {
                cmdId.CommandText = "SELECT last_insert_rowid();";
                object result = cmdId.ExecuteScalar();
                obj.GPID = Convert.ToInt64(result);
            }
        }

        private void Update(SQLiteConnection connection, T1GRPPO obj)
        {
            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "UPDATE T1GRPPO SET " +
                    "GRID = @GRID, " +
                    "PFID = @PFID, " +
                    "CRAT = @CRAT, " +
                    "CHAT = @CHAT " +
                    "WHERE GPID = @GPID;";

                cmd.Parameters.AddWithValue("@GPID", obj.GPID);

                cmd.Parameters.AddWithValue("@GRID", obj.GRID);
                cmd.Parameters.AddWithValue("@PFID", obj.PFID);
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

        private T1GRPPO Map(SQLiteDataReader reader)
        {
            T1GRPPO obj = new T1GRPPO();

            obj.GPID = Convert.ToInt64(reader.GetValue(0));
            obj.GRID = Convert.ToInt64(reader.GetValue(1));
            obj.PFID = Convert.ToInt64(reader.GetValue(2));

            obj.CRAT = ParseDbDateTime(reader.IsDBNull(3) ? null : reader.GetString(3));
            obj.CHAT = ParseDbDateTime(reader.IsDBNull(4) ? null : reader.GetString(4));

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