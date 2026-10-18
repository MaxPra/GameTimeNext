using System.Globalization;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase.Migration;
using System.Data.SQLite;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Migration.Types;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Metadata.Data
{
    public class TXMETAH
    {
        private SQLiteConnection _connection;

        public TXMETAH()
        {
            _connection = AppEnvironment.GetDataBaseManager().GetConnection();
        }

        public TXMETAH(SQLiteConnection connection)
        {
            _connection = connection;
        }

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
            copy.GENER = source.GENER;
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

            EnsureOpen(_connection);

            DateTime now = DateTime.Now;

            if (Exists(_connection, obj.MENAM))
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

        public void Delete(string menam)
        {
            EnsureOpen(_connection);

            using SQLiteCommand cmd = _connection.CreateCommand();

            cmd.CommandText =
                "DELETE FROM T1METAH " +
                "WHERE MENAM = @MENAM;";

            cmd.Parameters.AddWithValue("@MENAM", menam);

            cmd.ExecuteNonQuery();

            MigrationFactory.ToCsv.ExportCsvFileFor(_connection, "T1METAH", MigrationFactory.ImportType.DevSync);
        }

        public T1METAH Read(string menam)
        {
            EnsureOpen(_connection);

            using SQLiteCommand cmd = _connection.CreateCommand();

            cmd.CommandText =
                "SELECT MENAM, DESCR, MTYPE, DSYNC, GENER, CRAT, CRUS, CHAT, CHUS " +
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
            EnsureOpen(_connection);

            List<T1METAH> list = new List<T1METAH>();

            using SQLiteCommand cmd = _connection.CreateCommand();

            cmd.CommandText =
                "SELECT MENAM, DESCR, MTYPE, DSYNC, GENER, CRAT, CRUS, CHAT, CHUS " +
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
                "(MENAM, DESCR, MTYPE, DSYNC, GENER, CRAT, CRUS, CHAT, CHUS) " +
                "VALUES " +
                "(@MENAM, @DESCR, @MTYPE, @DSYNC, @GENER, @CRAT, @CRUS, @CHAT, @CHUS);";

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
                "GENER = @GENER, " +
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

            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        private void AddParameters(SQLiteCommand cmd, T1METAH obj)
        {
            cmd.Parameters.AddWithValue("@MENAM", obj.MENAM);
            cmd.Parameters.AddWithValue("@DESCR", obj.DESCR);
            cmd.Parameters.AddWithValue("@MTYPE", obj.MTYPE);
            cmd.Parameters.AddWithValue("@DSYNC", ToDbValue(obj.DSYNC));
            cmd.Parameters.AddWithValue("@GENER", ToDbValue(obj.GENER));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CRUS", obj.CRUS);
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@CHUS", obj.CHUS);
        }

        private T1METAH Map(SQLiteDataReader reader)
        {
            T1METAH obj = new T1METAH();

            obj.MENAM = (string?)FromDbValue(reader, 0) ?? string.Empty;
            obj.DESCR = (string?)FromDbValue(reader, 1) ?? string.Empty;
            obj.MTYPE = (string?)FromDbValue(reader, 2) ?? string.Empty;
            obj.DSYNC = (bool?)FromDbValue(reader, 3) ?? false;
            obj.GENER = (bool?)FromDbValue(reader, 4) ?? false;

            obj.CRAT = (DateTime?)FromDbValue(reader, 5) ?? DateTime.MinValue;
            obj.CRUS = (string?)FromDbValue(reader, 6) ?? string.Empty;
            obj.CHAT = (DateTime?)FromDbValue(reader, 7) ?? DateTime.MinValue;
            obj.CHUS = (string?)FromDbValue(reader, 8) ?? string.Empty;

            obj.State = UIXTableObjectState.Available;

            return obj;
        }

        private static object? FromDbValue(SQLiteDataReader reader, int ordinal)
        {
            UIXSqliteDataType dataType = MigrationFactory.Metadata.METADATA
                .Where(s => s.MENAM.Equals("T1METAH")).Single()
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
