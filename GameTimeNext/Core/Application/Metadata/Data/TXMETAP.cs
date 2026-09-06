using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase.Migration;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.Metadata.Data
{
    public class TXMETAP
    {
        private SQLiteConnection _connection;

        public TXMETAP()
        {
            _connection = AppEnvironment.GetDataBaseManager().GetConnection();
        }

        public TXMETAP(SQLiteConnection connection)
        {
            _connection = connection;
        }

        public T1METAP CreateNew()
        {
            T1METAP obj = new T1METAP();

            DateTime now = DateTime.Now;
            obj.CRAT = now;
            obj.CHAT = now;

            obj.State = UIXTableObjectState.New;

            return obj;
        }

        public T1METAP Copy(T1METAP source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            T1METAP copy = new T1METAP();

            copy.MENAM = source.MENAM;
            copy.PONAM = source.PONAM;
            copy.DESCR = source.DESCR;
            copy.DATYP = source.DATYP;
            copy.DALEN = source.DALEN;
            copy.PORDE = source.PORDE;
            copy.PRIMK = source.PRIMK;
            copy.AUTOI = source.AUTOI;
            copy.CRUS = source.CRUS;
            copy.CHUS = source.CHUS;

            DateTime now = DateTime.Now;
            copy.CRAT = now;
            copy.CHAT = now;

            copy.DEFAK = source.DEFAK;
            copy.DEFVL = source.DEFVL;

            copy.State = UIXTableObjectState.New;

            return copy;
        }

        public void Save(T1METAP obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            EnsureOpen(_connection);

            DateTime now = DateTime.Now;

            if (Exists(_connection, obj.MENAM, obj.PONAM))
            {
                obj.CHAT = now;
                Update(_connection, obj);
            }
            else
            {
                if (obj.CRAT == DateTime.MinValue)
                    obj.CRAT = now;

                obj.CHAT = now;

                Insert(_connection, obj);
            }

            obj.State = UIXTableObjectState.Available;
            obj.AcceptChanges();

            MigrationFactory.ToCsv.ExportCsvFileFor(_connection, obj, MigrationFactory.ImportType.DevSync);
        }

        public void Delete(string menam, string ponam)
        {
            EnsureOpen(_connection);

            using SQLiteCommand cmd = _connection.CreateCommand();

            cmd.CommandText =
                "DELETE FROM T1METAP " +
                "WHERE MENAM = @MENAM AND PONAM = @PONAM;";

            cmd.Parameters.AddWithValue("@MENAM", menam);
            cmd.Parameters.AddWithValue("@PONAM", ponam);

            cmd.ExecuteNonQuery();

            MigrationFactory.ToCsv.ExportCsvFileFor(_connection, "T1METAP", MigrationFactory.ImportType.DevSync);
        }

        public T1METAP Read(string menam, string ponam)
        {
            EnsureOpen(_connection);

            using SQLiteCommand cmd = _connection.CreateCommand();

            cmd.CommandText =
                "SELECT MENAM, PONAM, DESCR, DATYP, DALEN, PORDE, PRIMK, AUTOI, CRAT, CRUS, CHAT, CHUS, DEFAK, DEFVL " +
                "FROM T1METAP " +
                "WHERE MENAM = @MENAM AND PONAM = @PONAM;";

            cmd.Parameters.AddWithValue("@MENAM", menam);
            cmd.Parameters.AddWithValue("@PONAM", ponam);

            using SQLiteDataReader reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            T1METAP obj = Map(reader);
            obj.AcceptChanges();

            return obj;
        }

        public List<T1METAP> ReadAll()
        {
            return ReadAll(null);
        }

        public List<T1METAP> ReadAll(string? menam)
        {
            EnsureOpen(_connection);

            List<T1METAP> list = new List<T1METAP>();

            using SQLiteCommand cmd = _connection.CreateCommand();

            List<string> sqlLines = new List<string>()
            {
                "SELECT MENAM, PONAM, DESCR, DATYP, DALEN, PORDE, PRIMK, AUTOI, CRAT, CRUS, CHAT, CHUS, DEFAK, DEFVL ",
                "FROM T1METAP ",
            };

            if (menam is not null)
                sqlLines.Add($"WHERE MENAM = \"{menam}\"");

            sqlLines.Add("ORDER BY MENAM, PORDE, PONAM;");

            cmd.CommandText = string.Join(Environment.NewLine, sqlLines);

            using SQLiteDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                T1METAP obj = Map(reader);
                obj.AcceptChanges();

                list.Add(obj);
            }

            return list;
        }

        private void Insert(SQLiteConnection connection, T1METAP obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "INSERT INTO T1METAP " +
                "(MENAM, PONAM, DESCR, DATYP, DALEN, PORDE, PRIMK, AUTOI, CRAT, CRUS, CHAT, CHUS, DEFAK, DEFVL) " +
                "VALUES " +
                "(@MENAM, @PONAM, @DESCR, @DATYP, @DALEN, @PORDE, @PRIMK, @AUTOI, @CRAT, @CRUS, @CHAT, @CHUS, @DEFAK, @DEFVL);";

            AddParameters(cmd, obj);

            cmd.ExecuteNonQuery();
        }

        private void Update(SQLiteConnection connection, T1METAP obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "UPDATE T1METAP SET " +
                "DESCR = @DESCR, " +
                "DATYP = @DATYP, " +
                "DALEN = @DALEN, " +
                "PORDE = @PORDE, " +
                "PRIMK = @PRIMK, " +
                "AUTOI = @AUTOI, " +
                "CRAT = @CRAT, " +
                "CRUS = @CRUS, " +
                "CHAT = @CHAT, " +
                "CHUS = @CHUS " +
                "DEFAK = @DEFAK " +
                "DEFVL = @DEFVL " +
                "WHERE MENAM = @MENAM AND PONAM = @PONAM;";

            AddParameters(cmd, obj);

            cmd.ExecuteNonQuery();
        }

        private bool Exists(SQLiteConnection connection, string menam, string ponam)
        {
            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "SELECT COUNT(*) " +
                "FROM T1METAP " +
                "WHERE MENAM = @MENAM AND PONAM = @PONAM;";

            cmd.Parameters.AddWithValue("@MENAM", menam);
            cmd.Parameters.AddWithValue("@PONAM", ponam);

            return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        private void AddParameters(SQLiteCommand cmd, T1METAP obj)
        {
            cmd.Parameters.AddWithValue("@MENAM", obj.MENAM);
            cmd.Parameters.AddWithValue("@PONAM", obj.PONAM);
            cmd.Parameters.AddWithValue("@DESCR", obj.DESCR);
            cmd.Parameters.AddWithValue("@DATYP", obj.DATYP);
            cmd.Parameters.AddWithValue("@DALEN", obj.DALEN);
            cmd.Parameters.AddWithValue("@PORDE", obj.PORDE);
            cmd.Parameters.AddWithValue("@PRIMK", obj.PRIMK ? 1 : 0);
            cmd.Parameters.AddWithValue("@AUTOI", obj.AUTOI ? 1 : 0);
            cmd.Parameters.AddWithValue("@CRAT", ToDbDateTime(obj.CRAT));
            cmd.Parameters.AddWithValue("@CRUS", obj.CRUS);
            cmd.Parameters.AddWithValue("@CHAT", ToDbDateTime(obj.CHAT));
            cmd.Parameters.AddWithValue("@CHUS", obj.CHUS);
            cmd.Parameters.AddWithValue("@DEFAK", obj.DEFAK);
            cmd.Parameters.AddWithValue("@DEFVL", obj.DEFVL);
        }

        private T1METAP Map(SQLiteDataReader reader)
        {
            T1METAP obj = new T1METAP();

            obj.MENAM = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            obj.PONAM = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            obj.DESCR = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            obj.DATYP = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
            obj.DALEN = reader.IsDBNull(4) ? 0 : Convert.ToInt32(reader.GetValue(4));
            obj.PORDE = reader.IsDBNull(5) ? 0 : Convert.ToInt32(reader.GetValue(5));
            obj.PRIMK = !reader.IsDBNull(6) && Convert.ToInt32(reader.GetValue(6)) == 1;
            obj.AUTOI = !reader.IsDBNull(7) && Convert.ToInt32(reader.GetValue(7)) == 1;

            obj.CRAT = ParseDbDateTime(reader.IsDBNull(8) ? null : reader.GetString(8));
            obj.CRUS = reader.IsDBNull(9) ? string.Empty : reader.GetString(9);
            obj.CHAT = ParseDbDateTime(reader.IsDBNull(10) ? null : reader.GetString(10));
            obj.CHUS = reader.IsDBNull(11) ? string.Empty : reader.GetString(11);
            obj.DEFAK = !reader.IsDBNull(12) && Convert.ToInt32(reader.GetValue(12)) == 1;
            obj.DEFVL = reader.IsDBNull(13) ? string.Empty : reader.GetString(13);

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
