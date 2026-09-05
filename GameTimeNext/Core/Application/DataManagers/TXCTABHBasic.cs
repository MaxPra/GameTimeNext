using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase.Migration;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXCTABHBasic
    {
        public virtual T1CTABH CreateNew()
        {
            T1CTABH obj = new T1CTABH();
            obj.State = UIXTableObjectState.New;
            return obj;
        }

        public virtual void Save(T1CTABH obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            if (Exists(connection, obj))
                Update(connection, obj);
            else
                Insert(connection, obj);

            obj.State = UIXTableObjectState.Available;
            obj.AcceptChanges();
            MigrationFactory.ToCsv.ExportCsvFileFor(connection, obj);
        }

        public virtual void Delete(string tXTYP)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM T1CTABH WHERE TXTYP = @TXTYP";
            cmd.Parameters.AddWithValue("@TXTYP", tXTYP);
            cmd.ExecuteNonQuery();
            MigrationFactory.ToCsv.ExportCsvFileFor(connection, "T1CTABH");
        }

        public virtual T1CTABH? Read(string tXTYP)
        {
            UIXQuery query = new UIXQuery(K1CTABH.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddField(K1CTABH.Name, K1CTABH.Fields.TXTYP);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.DESCR);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PERMI);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PAAC1);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PADE1);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PARF1);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PACO1);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PACT1);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PAAC2);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PADE2);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PARF2);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PACO2);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PACT2);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.CRAT);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.CHAT);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.NRANA);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.EXPRT);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PTOL1);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PTOL2);
            query.AddWhere(K1CTABH.Name, K1CTABH.Fields.TXTYP, QueryCompareType.EQUALS, tXTYP);

            using var reader = query.Execute();
            if (!reader.Read())
                return null;

            T1CTABH obj = Map(reader);
            obj.AcceptChanges();
            return obj;
        }

        public virtual List<T1CTABH> ReadAll()
        {
            UIXQuery query = new UIXQuery(K1CTABH.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddField(K1CTABH.Name, K1CTABH.Fields.TXTYP);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.DESCR);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PERMI);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PAAC1);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PADE1);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PARF1);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PACO1);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PACT1);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PAAC2);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PADE2);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PARF2);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PACO2);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PACT2);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.CRAT);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.CHAT);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.NRANA);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.EXPRT);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PTOL1);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PTOL2);
            query.AddOrderBy(K1CTABH.Name, K1CTABH.Fields.TXTYP, OrderDirection.ASC);

            List<T1CTABH> list = new List<T1CTABH>();
            using var reader = query.Execute();
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
            DateTime now = DateTime.Now;
            obj.CRAT = now;
            obj.CHAT = now;
            cmd.CommandText = "INSERT INTO T1CTABH (TXTYP, DESCR, PERMI, PAAC1, PADE1, PARF1, PACO1, PACT1, PAAC2, PADE2, PARF2, PACO2, PACT2, CRAT, CHAT, NRANA, EXPRT, PTOL1, PTOL2) VALUES (@TXTYP, @DESCR, @PERMI, @PAAC1, @PADE1, @PARF1, @PACO1, @PACT1, @PAAC2, @PADE2, @PARF2, @PACO2, @PACT2, @CRAT, @CHAT, @NRANA, @EXPRT, @PTOL1, @PTOL2)";
            cmd.Parameters.AddWithValue("@TXTYP", ToDbValue(obj.TXTYP));
            cmd.Parameters.AddWithValue("@DESCR", ToDbValue(obj.DESCR));
            cmd.Parameters.AddWithValue("@PERMI", ToDbValue(obj.PERMI));
            cmd.Parameters.AddWithValue("@PAAC1", ToDbValue(obj.PAAC1));
            cmd.Parameters.AddWithValue("@PADE1", ToDbValue(obj.PADE1));
            cmd.Parameters.AddWithValue("@PARF1", ToDbValue(obj.PARF1));
            cmd.Parameters.AddWithValue("@PACO1", ToDbValue(obj.PACO1));
            cmd.Parameters.AddWithValue("@PACT1", ToDbValue(obj.PACT1));
            cmd.Parameters.AddWithValue("@PAAC2", ToDbValue(obj.PAAC2));
            cmd.Parameters.AddWithValue("@PADE2", ToDbValue(obj.PADE2));
            cmd.Parameters.AddWithValue("@PARF2", ToDbValue(obj.PARF2));
            cmd.Parameters.AddWithValue("@PACO2", ToDbValue(obj.PACO2));
            cmd.Parameters.AddWithValue("@PACT2", ToDbValue(obj.PACT2));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@NRANA", ToDbValue(obj.NRANA));
            cmd.Parameters.AddWithValue("@EXPRT", ToDbValue(obj.EXPRT));
            cmd.Parameters.AddWithValue("@PTOL1", ToDbValue(obj.PTOL1));
            cmd.Parameters.AddWithValue("@PTOL2", ToDbValue(obj.PTOL2));
            cmd.ExecuteNonQuery();
        }

        private void Update(SQLiteConnection connection, T1CTABH obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();
            obj.CHAT = DateTime.Now;
            cmd.CommandText = "UPDATE T1CTABH SET DESCR = @DESCR, PERMI = @PERMI, PAAC1 = @PAAC1, PADE1 = @PADE1, PARF1 = @PARF1, PACO1 = @PACO1, PACT1 = @PACT1, PAAC2 = @PAAC2, PADE2 = @PADE2, PARF2 = @PARF2, PACO2 = @PACO2, PACT2 = @PACT2, CRAT = @CRAT, CHAT = @CHAT, NRANA = @NRANA, EXPRT = @EXPRT, PTOL1 = @PTOL1, PTOL2 = @PTOL2 WHERE TXTYP = @TXTYP";
            cmd.Parameters.AddWithValue("@TXTYP", ToDbValue(obj.TXTYP));
            cmd.Parameters.AddWithValue("@DESCR", ToDbValue(obj.DESCR));
            cmd.Parameters.AddWithValue("@PERMI", ToDbValue(obj.PERMI));
            cmd.Parameters.AddWithValue("@PAAC1", ToDbValue(obj.PAAC1));
            cmd.Parameters.AddWithValue("@PADE1", ToDbValue(obj.PADE1));
            cmd.Parameters.AddWithValue("@PARF1", ToDbValue(obj.PARF1));
            cmd.Parameters.AddWithValue("@PACO1", ToDbValue(obj.PACO1));
            cmd.Parameters.AddWithValue("@PACT1", ToDbValue(obj.PACT1));
            cmd.Parameters.AddWithValue("@PAAC2", ToDbValue(obj.PAAC2));
            cmd.Parameters.AddWithValue("@PADE2", ToDbValue(obj.PADE2));
            cmd.Parameters.AddWithValue("@PARF2", ToDbValue(obj.PARF2));
            cmd.Parameters.AddWithValue("@PACO2", ToDbValue(obj.PACO2));
            cmd.Parameters.AddWithValue("@PACT2", ToDbValue(obj.PACT2));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@NRANA", ToDbValue(obj.NRANA));
            cmd.Parameters.AddWithValue("@EXPRT", ToDbValue(obj.EXPRT));
            cmd.Parameters.AddWithValue("@PTOL1", ToDbValue(obj.PTOL1));
            cmd.Parameters.AddWithValue("@PTOL2", ToDbValue(obj.PTOL2));
            cmd.ExecuteNonQuery();
        }

        private bool Exists(SQLiteConnection connection, T1CTABH obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM T1CTABH WHERE TXTYP = @TXTYP";
            cmd.Parameters.AddWithValue("@TXTYP", ToDbValue(obj.TXTYP));
            return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        protected static T1CTABH Map(SQLiteDataReader reader)
        {
            T1CTABH obj = new T1CTABH();
            obj.TXTYP = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            obj.DESCR = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            obj.PERMI = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            obj.PAAC1 = !reader.IsDBNull(3) && Convert.ToInt32(reader.GetValue(3)) == 1;
            obj.PADE1 = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
            obj.PARF1 = !reader.IsDBNull(5) && Convert.ToInt32(reader.GetValue(5)) == 1;
            obj.PACO1 = reader.IsDBNull(6) ? string.Empty : reader.GetString(6);
            obj.PACT1 = reader.IsDBNull(7) ? string.Empty : reader.GetString(7);
            obj.PAAC2 = !reader.IsDBNull(8) && Convert.ToInt32(reader.GetValue(8)) == 1;
            obj.PADE2 = reader.IsDBNull(9) ? string.Empty : reader.GetString(9);
            obj.PARF2 = !reader.IsDBNull(10) && Convert.ToInt32(reader.GetValue(10)) == 1;
            obj.PACO2 = reader.IsDBNull(11) ? string.Empty : reader.GetString(11);
            obj.PACT2 = reader.IsDBNull(12) ? string.Empty : reader.GetString(12);
            obj.CRAT = ParseDbDateTime(reader.GetValue(13));
            obj.CHAT = ParseDbDateTime(reader.GetValue(14));
            obj.NRANA = !reader.IsDBNull(15) && Convert.ToInt32(reader.GetValue(15)) == 1;
            obj.EXPRT = !reader.IsDBNull(16) && Convert.ToInt32(reader.GetValue(16)) == 1;
            obj.PTOL1 = reader.IsDBNull(17) ? string.Empty : reader.GetString(17);
            obj.PTOL2 = reader.IsDBNull(18) ? string.Empty : reader.GetString(18);
            obj.State = UIXTableObjectState.Available;
            return obj;
        }

        private static object ToDbValue(object? value)
        {
            if (value is bool boolValue)
                return boolValue ? 1 : 0;
            if (value is DateTime dt)
                return dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            return value ?? DBNull.Value;
        }

        private static DateTime ParseDbDateTime(object? value)
        {
            if (value == null || value == DBNull.Value)
                return DateTime.MinValue;

            string raw = value.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
                return DateTime.MinValue;

            if (DateTime.TryParseExact(raw, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
                return parsed;
            if (DateTime.TryParseExact(raw, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                return parsed;
            if (DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsed))
                return parsed;
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                return parsed;

            return DateTime.MinValue;
        }

        protected static void EnsureOpen(SQLiteConnection connection)
        {
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();
        }
    }
}
