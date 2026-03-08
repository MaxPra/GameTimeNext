using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.DataManagers
{
    internal class TXGROUP
    {
        public T1GROUP CreateNew()
        {
            T1GROUP obj = new T1GROUP();

            DateTime now = DateTime.Now;
            obj.CRAT = now;
            obj.CHAT = now;

            obj.State = UIXTableObjectState.New;

            return obj;
        }

        public T1GROUP Copy(T1GROUP source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            T1GROUP copy = new T1GROUP();

            copy.GRID = 0;
            copy.GRNA = source.GRNA;
            copy.GTYP = source.GTYP;

            DateTime now = DateTime.Now;
            copy.CRAT = now;
            copy.CHAT = now;

            copy.State = UIXTableObjectState.New;

            return copy;
        }

        public void Save(T1GROUP obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            DateTime now = DateTime.Now;

            if (obj.GRID <= 0)
            {
                if (obj.CRAT == DateTime.MinValue) obj.CRAT = now;
                obj.CHAT = now;


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

        public void Delete(long grid)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM T1GROUP WHERE GRID = @GRID;";
                cmd.Parameters.AddWithValue("@GRID", grid);
                cmd.ExecuteNonQuery();
            }
        }

        public List<T1GROUP> ReadAll()
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            List<T1GROUP> list = new List<T1GROUP>();

            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT GRID, GRNA, GTYP, CRAT, CHAT FROM T1GROUP ORDER BY GRID;";

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        T1GROUP obj = Map(reader);
                        obj.AcceptChanges();
                        list.Add(obj);
                    }
                }
            }

            return list;
        }

        public T1GROUP Read(long grid)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT GRID, GRNA, GTYP, CRAT, CHAT FROM T1GROUP WHERE GRID = @GRID;";
                cmd.Parameters.AddWithValue("@GRID", grid);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    T1GROUP obj = Map(reader);
                    obj.AcceptChanges();
                    return obj;
                }
            }
        }

        private void Insert(SQLiteConnection connection, T1GROUP obj)
        {
            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "INSERT INTO T1GROUP (GRNA, CRAT, GTYP, CHAT) " +
                    "VALUES (@GRNA, @CRAT, @CHAT);";

                cmd.Parameters.AddWithValue("@GRNA", obj.GRNA);
                cmd.Parameters.AddWithValue("@GTYP", obj.GTYP);
                cmd.Parameters.AddWithValue("@CRAT", ToDbDateTime(obj.CRAT));
                cmd.Parameters.AddWithValue("@CHAT", ToDbDateTime(obj.CHAT));

                cmd.ExecuteNonQuery();
            }

            using (SQLiteCommand cmdId = connection.CreateCommand())
            {
                cmdId.CommandText = "SELECT last_insert_rowid();";
                object result = cmdId.ExecuteScalar();
                obj.GRID = Convert.ToInt64(result);
            }
        }

        private void Update(SQLiteConnection connection, T1GROUP obj)
        {
            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "UPDATE T1GROUP SET " +
                    "GRNA = @GRNA, " +
                    "GTYP = @GTYP," +
                    "CRAT = @CRAT, " +
                    "CHAT = @CHAT " +
                    "WHERE GRID = @GRID;";

                cmd.Parameters.AddWithValue("@GRID", obj.GRID);

                cmd.Parameters.AddWithValue("@GRNA", obj.GRNA);
                cmd.Parameters.AddWithValue("@GTYP", obj.GTYP);
                cmd.Parameters.AddWithValue("@CRAT", ToDbDateTime(obj.CRAT));
                cmd.Parameters.AddWithValue("@CHAT", ToDbDateTime(obj.CHAT));

                cmd.ExecuteNonQuery();
            }
        }

        private void EnsureOpen(SQLiteConnection connection)
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }
        }

        private string ToDbDateTime(DateTime value)
        {
            return value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private T1GROUP Map(SQLiteDataReader reader)
        {
            T1GROUP obj = new T1GROUP();

            obj.GRID = Convert.ToInt64(reader.GetValue(0));
            obj.GRNA = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            obj.GTYP = reader.IsDBNull(1) ? string.Empty : reader.GetString(2);

            obj.CRAT = ParseDbDateTime(reader.IsDBNull(2) ? null : reader.GetString(2));
            obj.CHAT = ParseDbDateTime(reader.IsDBNull(3) ? null : reader.GetString(3));

            obj.State = UIXTableObjectState.Available;

            return obj;
        }

        private DateTime ParseDbDateTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DateTime.MinValue;
            }

            DateTime result;
            if (DateTime.TryParseExact(
                    value,
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out result))
            {
                return result;
            }

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return result;
            }

            return DateTime.MinValue;
        }

    }
}
