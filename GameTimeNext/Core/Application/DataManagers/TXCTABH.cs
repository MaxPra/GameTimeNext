using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXCTABH
    {
        public T1CTABH CreateNew()
        {
            T1CTABH obj = new T1CTABH();

            DateTime now = DateTime.Now;

            obj.CRAT = now;
            obj.CHAT = now;

            obj.State = UIXTableObjectState.New;

            return obj;
        }

        public T1CTABH Copy(T1CTABH source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            T1CTABH copy = new T1CTABH();

            copy.TXTYP = source.TXTYP;
            copy.DESCR = source.DESCR;
            copy.PERMI = source.PERMI;

            DateTime now = DateTime.Now;
            copy.CRAT = now;
            copy.CHAT = now;

            copy.State = UIXTableObjectState.New;

            return copy;
        }

        public void Save(T1CTABH obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            if (Exists(connection, obj.TXTYP))
            {
                obj.CHAT = DateTime.Now;
                Update(connection, obj);
            }
            else
            {
                if (obj.CRAT == DateTime.MinValue)
                    obj.CRAT = DateTime.Now;

                obj.CHAT = DateTime.Now;

                Insert(connection, obj);
            }

            obj.State = UIXTableObjectState.Available;
            obj.AcceptChanges();
        }

        public void Delete(string txtyp)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "DELETE FROM T1CTABH WHERE TXTYP = @TXTYP;";

            cmd.Parameters.AddWithValue("@TXTYP", txtyp);

            cmd.ExecuteNonQuery();
        }

        public T1CTABH Read(string txtyp)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "SELECT TXTYP, DESCR, PERMI, CRAT, CHAT " +
                "FROM T1CTABH " +
                "WHERE TXTYP = @TXTYP;";

            cmd.Parameters.AddWithValue("@TXTYP", txtyp);

            using SQLiteDataReader reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            T1CTABH obj = Map(reader);
            obj.AcceptChanges();

            return obj;
        }

        public List<T1CTABH> ReadAll()
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            List<T1CTABH> list = new();

            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "SELECT TXTYP, DESCR, PERMI, CRAT, CHAT " +
                "FROM T1CTABH " +
                "ORDER BY TXTYP;";

            using SQLiteDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                T1CTABH obj = Map(reader);

                obj.AcceptChanges();

                list.Add(obj);
            }

            return list;
        }

        public List<T1CTABD> GetEntries(string txtyp)
        {
            UIXQuery query = new UIXQuery(
                K1CTABD.Name,
                AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddWhere(
                K1CTABD.Name,
                K1CTABD.Fields.TXTYP,
                QueryCompareType.EQUALS,
                txtyp);

            query.AddOrderBy(
                K1CTABD.Name,
                K1CTABD.Fields.DESCR,
                OrderDirection.ASC);

            List<T1CTABD> entries = new();

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    T1CTABD entry = new T1CTABD();

                    entry.TXTYP = UIXQuery.GetString(reader, K1CTABD.Name, K1CTABD.Fields.TXTYP, string.Empty);
                    entry.TXNUM = UIXQuery.GetString(reader, K1CTABD.Name, K1CTABD.Fields.TXNUM, string.Empty);
                    entry.DESCR = UIXQuery.GetString(reader, K1CTABD.Name, K1CTABD.Fields.DESCR, string.Empty);

                    entries.Add(entry);
                }
            }

            return entries;
        }

        private void Insert(SQLiteConnection connection, T1CTABH obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "INSERT INTO T1CTABH " +
                "(TXTYP, DESCR, PERMI, CRAT, CHAT) " +
                "VALUES " +
                "(@TXTYP, @DESCR, @PERMI, @CRAT, @CHAT);";

            cmd.Parameters.AddWithValue("@TXTYP", obj.TXTYP);
            cmd.Parameters.AddWithValue("@DESCR", obj.DESCR);
            cmd.Parameters.AddWithValue("@PERMI", obj.PERMI);
            cmd.Parameters.AddWithValue("@CRAT", ToDbDateTime(obj.CRAT));
            cmd.Parameters.AddWithValue("@CHAT", ToDbDateTime(obj.CHAT));

            cmd.ExecuteNonQuery();
        }

        private void Update(SQLiteConnection connection, T1CTABH obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "UPDATE T1CTABH SET " +
                "DESCR = @DESCR, " +
                "PERMI = @PERMI, " +
                "CHAT = @CHAT " +
                "WHERE TXTYP = @TXTYP;";

            cmd.Parameters.AddWithValue("@TXTYP", obj.TXTYP);
            cmd.Parameters.AddWithValue("@DESCR", obj.DESCR);
            cmd.Parameters.AddWithValue("@PERMI", obj.PERMI);
            cmd.Parameters.AddWithValue("@CHAT", ToDbDateTime(obj.CHAT));

            cmd.ExecuteNonQuery();
        }

        private bool Exists(SQLiteConnection connection, string txtyp)
        {
            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "SELECT COUNT(*) FROM T1CTABH WHERE TXTYP = @TXTYP;";

            cmd.Parameters.AddWithValue("@TXTYP", txtyp);

            return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        private T1CTABH Map(SQLiteDataReader reader)
        {
            T1CTABH obj = new T1CTABH();

            obj.TXTYP = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            obj.DESCR = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            obj.PERMI = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);

            obj.CRAT = ParseDbDateTime(reader.IsDBNull(3) ? null : reader.GetString(3));
            obj.CHAT = ParseDbDateTime(reader.IsDBNull(4) ? null : reader.GetString(4));

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

            if (DateTime.TryParseExact(value,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime result))
            {
                return result;
            }

            return DateTime.MinValue;
        }
    }
}