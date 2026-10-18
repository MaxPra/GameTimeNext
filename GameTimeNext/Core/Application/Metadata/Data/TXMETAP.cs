using System.Globalization;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase.Migration;
using System.Data.SQLite;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Migration.Types;
using UIX.ViewController.Engine.Utils;

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
                "CHUS = @CHUS, " +
                "DEFAK = @DEFAK, " +
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

            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        private void AddParameters(SQLiteCommand cmd, T1METAP obj)
        {
            cmd.Parameters.AddWithValue("@MENAM", obj.MENAM);
            cmd.Parameters.AddWithValue("@PONAM", obj.PONAM);
            cmd.Parameters.AddWithValue("@DESCR", obj.DESCR);
            cmd.Parameters.AddWithValue("@DATYP", obj.DATYP);
            cmd.Parameters.AddWithValue("@DALEN", obj.DALEN);
            cmd.Parameters.AddWithValue("@PORDE", obj.PORDE);
            cmd.Parameters.AddWithValue("@PRIMK", ToDbValue(obj.PRIMK));
            cmd.Parameters.AddWithValue("@AUTOI", ToDbValue(obj.AUTOI));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CRUS", obj.CRUS);
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@CHUS", obj.CHUS);
            cmd.Parameters.AddWithValue("@DEFAK", ToDbValue(obj.DEFAK));
            cmd.Parameters.AddWithValue("@DEFVL", obj.DEFVL);
        }

        private T1METAP Map(SQLiteDataReader reader)
        {
            T1METAP obj = new T1METAP();

            obj.MENAM = (string?)FromDbValue(reader, 0) ?? string.Empty;
            obj.PONAM = (string?)FromDbValue(reader, 1) ?? string.Empty;
            obj.DESCR = (string?)FromDbValue(reader, 2) ?? string.Empty;
            obj.DATYP = (string?)FromDbValue(reader, 3) ?? string.Empty;
            obj.DALEN = (int?)FromDbValue(reader, 4) ?? 0;
            obj.PORDE = (int?)FromDbValue(reader, 5) ?? 0;
            obj.PRIMK = (bool?)FromDbValue(reader, 6) ?? false;
            obj.AUTOI = (bool?)FromDbValue(reader, 7) ?? false;

            obj.CRAT = (DateTime?)FromDbValue(reader, 8) ?? DateTime.MinValue;
            obj.CRUS = (string?)FromDbValue(reader, 9) ?? string.Empty;
            obj.CHAT = (DateTime?)FromDbValue(reader, 10) ?? DateTime.MinValue;
            obj.CHUS = (string?)FromDbValue(reader, 11) ?? string.Empty;
            obj.DEFAK = (bool?)FromDbValue(reader, 12) ?? false;
            obj.DEFVL = (string?)FromDbValue(reader, 13) ?? string.Empty;

            obj.State = UIXTableObjectState.Available;

            return obj;
        }

        private static object? FromDbValue(SQLiteDataReader reader, int ordinal)
        {
            UIXSqliteDataType dataType = MigrationFactory.Metadata.METADATA
                .Where(s => s.MENAM.Equals("T1METAP")).Single()
                .Columns.Where(c => c.PONAM.Equals(reader.GetName(ordinal), StringComparison.OrdinalIgnoreCase)).Single().DATYP;

            object value;
            if (reader.IsDBNull(ordinal))
                value = DBNull.Value;
            else if (dataType.Key.Equals("05"))
                value = reader.GetString(ordinal); // Raw string, the conversion parses it
            else
                value = reader.GetValue(ordinal);

            return UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.Sqlite, UIXSqliteDataType.ConversionType.CSharp, value, dataType);
        }

        private static object? ToDbValue(object? valFrom)
        {
            return UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.CSharp, UIXSqliteDataType.ConversionType.Sqlite, valFrom);
        }

        private void EnsureOpen(SQLiteConnection connection)
        {
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();
        }

    }
}
