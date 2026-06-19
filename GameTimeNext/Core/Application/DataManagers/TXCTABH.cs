using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase.DevSync;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.DataBaseObjects;

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

            copy.PAAC1 = source.PAAC1;
            copy.PADE1 = source.PADE1;
            copy.PARF1 = source.PARF1;
            copy.PACO1 = source.PACO1;
            copy.PACT1 = source.PACT1;

            copy.PAAC2 = source.PAAC2;
            copy.PADE2 = source.PADE2;
            copy.PARF2 = source.PARF2;
            copy.PACO2 = source.PACO2;
            copy.PACT2 = source.PACT2;

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

            DateTime now = DateTime.Now;

            if (Exists(connection, obj.TXTYP))
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

        public void Delete(string txtyp)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "DELETE FROM T1CTABH " +
                "WHERE TXTYP = @TXTYP;";

            cmd.Parameters.AddWithValue("@TXTYP", txtyp);

            cmd.ExecuteNonQuery();

            DevSyncCsvSyncService.ExportTable("T1CTABH");
        }

        public T1CTABH Read(string txtyp)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "SELECT TXTYP, DESCR, PERMI, CRAT, CHAT, " +
                "PAAC1, PADE1, PARF1, PACO1, PACT1, " +
                "PAAC2, PADE2, PARF2, PACO2, PACT2 " +
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

            List<T1CTABH> list = new List<T1CTABH>();

            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "SELECT TXTYP, DESCR, PERMI, CRAT, CHAT, " +
                "PAAC1, PADE1, PARF1, PACO1, PACT1, " +
                "PAAC2, PADE2, PARF2, PACO2, PACT2 " +
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

        public List<T1CTABH> GetEntries(string txtyp)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);
            List<T1CTABH> list = new List<T1CTABH>();
            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText =
                "SELECT TXTYP, DESCR, PERMI, CRAT, CHAT, " +
                "PAAC1, PADE1, PARF1, PACO1, PACT1, " +
                "PAAC2, PADE2, PARF2, PACO2, PACT2 " +
                "FROM T1CTABH " +
                "WHERE TXTYP = @TXTYP " +
                "ORDER BY TXTYP;";
            cmd.Parameters.AddWithValue("@TXTYP", txtyp);
            using SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                T1CTABH obj = Map(reader);
                obj.AcceptChanges();
                list.Add(obj);
            }
            return list;
        }

        private void Insert(SQLiteConnection connection, T1CTABH obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "INSERT INTO T1CTABH " +
                "(TXTYP, DESCR, PERMI, CRAT, CHAT, " +
                "PAAC1, PADE1, PARF1, PACO1, PACT1, " +
                "PAAC2, PADE2, PARF2, PACO2, PACT2) " +
                "VALUES " +
                "(@TXTYP, @DESCR, @PERMI, @CRAT, @CHAT, " +
                "@PAAC1, @PADE1, @PARF1, @PACO1, @PACT1, " +
                "@PAAC2, @PADE2, @PARF2, @PACO2, @PACT2);";

            AddParameters(cmd, obj);

            cmd.ExecuteNonQuery();
        }

        private void Update(SQLiteConnection connection, T1CTABH obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "UPDATE T1CTABH SET " +
                "DESCR = @DESCR, " +
                "PERMI = @PERMI, " +
                "CHAT = @CHAT, " +
                "PAAC1 = @PAAC1, " +
                "PADE1 = @PADE1, " +
                "PARF1 = @PARF1, " +
                "PACO1 = @PACO1, " +
                "PACT1 = @PACT1, " +
                "PAAC2 = @PAAC2, " +
                "PADE2 = @PADE2, " +
                "PARF2 = @PARF2, " +
                "PACO2 = @PACO2, " +
                "PACT2 = @PACT2 " +
                "WHERE TXTYP = @TXTYP;";

            AddParameters(cmd, obj);

            cmd.ExecuteNonQuery();
        }

        private bool Exists(SQLiteConnection connection, string txtyp)
        {
            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "SELECT COUNT(*) " +
                "FROM T1CTABH " +
                "WHERE TXTYP = @TXTYP;";

            cmd.Parameters.AddWithValue("@TXTYP", txtyp);

            return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        private void AddParameters(SQLiteCommand cmd, T1CTABH obj)
        {
            cmd.Parameters.AddWithValue("@TXTYP", obj.TXTYP);
            cmd.Parameters.AddWithValue("@DESCR", obj.DESCR);
            cmd.Parameters.AddWithValue("@PERMI", obj.PERMI);
            cmd.Parameters.AddWithValue("@CRAT", ToDbDateTime(obj.CRAT));
            cmd.Parameters.AddWithValue("@CHAT", ToDbDateTime(obj.CHAT));

            cmd.Parameters.AddWithValue("@PAAC1", obj.PAAC1 ? 1 : 0);
            cmd.Parameters.AddWithValue("@PADE1", obj.PADE1);
            cmd.Parameters.AddWithValue("@PARF1", obj.PARF1 ? 1 : 0);
            cmd.Parameters.AddWithValue("@PACO1", obj.PACO1);
            cmd.Parameters.AddWithValue("@PACT1", obj.PACT1);

            cmd.Parameters.AddWithValue("@PAAC2", obj.PAAC2 ? 1 : 0);
            cmd.Parameters.AddWithValue("@PADE2", obj.PADE2);
            cmd.Parameters.AddWithValue("@PARF2", obj.PARF2 ? 1 : 0);
            cmd.Parameters.AddWithValue("@PACO2", obj.PACO2);
            cmd.Parameters.AddWithValue("@PACT2", obj.PACT2);
        }

        private T1CTABH Map(SQLiteDataReader reader)
        {
            T1CTABH obj = new T1CTABH();

            obj.TXTYP = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            obj.DESCR = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            obj.PERMI = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);

            obj.CRAT = ParseDbDateTime(reader.IsDBNull(3) ? null : reader.GetString(3));
            obj.CHAT = ParseDbDateTime(reader.IsDBNull(4) ? null : reader.GetString(4));

            obj.PAAC1 = !reader.IsDBNull(5) && Convert.ToInt32(reader.GetValue(5)) == 1;
            obj.PADE1 = reader.IsDBNull(6) ? string.Empty : reader.GetString(6);
            obj.PARF1 = !reader.IsDBNull(7) && Convert.ToInt32(reader.GetValue(7)) == 1;
            obj.PACO1 = reader.IsDBNull(8) ? string.Empty : reader.GetString(8);
            obj.PACT1 = reader.IsDBNull(9) ? string.Empty : reader.GetString(9);

            obj.PAAC2 = !reader.IsDBNull(10) && Convert.ToInt32(reader.GetValue(10)) == 1;
            obj.PADE2 = reader.IsDBNull(11) ? string.Empty : reader.GetString(11);
            obj.PARF2 = !reader.IsDBNull(12) && Convert.ToInt32(reader.GetValue(12)) == 1;
            obj.PACO2 = reader.IsDBNull(13) ? string.Empty : reader.GetString(13);
            obj.PACT2 = reader.IsDBNull(14) ? string.Empty : reader.GetString(14);

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