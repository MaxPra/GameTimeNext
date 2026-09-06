using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase.Migration;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXPROFIBasic
    {
        public virtual T1PROFI CreateNew()
        {
            T1PROFI obj = new T1PROFI();
            obj.State = UIXTableObjectState.New;
            return obj;
        }

        public virtual void Save(T1PROFI obj)
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
            MigrationFactory.ToCsv.ExportCsvFileFor(connection, obj, MigrationFactory.ImportType.DevSync);
        }

        public virtual void Delete(long pFID)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM T1PROFI WHERE PFID = @PFID";
            cmd.Parameters.AddWithValue("@PFID", pFID);
            cmd.ExecuteNonQuery();
            MigrationFactory.ToCsv.ExportCsvFileFor(connection, "T1PROFI", MigrationFactory.ImportType.DevSync);
        }

        public virtual T1PROFI? Read(long pFID)
        {
            UIXQuery query = new UIXQuery(K1PROFI.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddField(K1PROFI.Name, K1PROFI.Fields.PFID);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.GANA);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.FIPL);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.LAPL);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.PPFN);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.EXGF);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.SAID);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.PRSE);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.EXEC);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.CRAT);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.CHAT);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ACCO);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ACIN);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ACAC);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.CUPT);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ETMA);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ETME);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ETCO);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ETTY);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ETML);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ARCH);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.PLAFO);
            query.AddWhere(K1PROFI.Name, K1PROFI.Fields.PFID, QueryCompareType.EQUALS, pFID);

            using var reader = query.Execute();
            if (!reader.Read())
                return null;

            T1PROFI obj = Map(reader);
            obj.AcceptChanges();
            return obj;
        }

        public virtual List<T1PROFI> ReadAll()
        {
            UIXQuery query = new UIXQuery(K1PROFI.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddField(K1PROFI.Name, K1PROFI.Fields.PFID);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.GANA);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.FIPL);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.LAPL);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.PPFN);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.EXGF);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.SAID);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.PRSE);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.EXEC);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.CRAT);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.CHAT);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ACCO);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ACIN);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ACAC);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.CUPT);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ETMA);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ETME);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ETCO);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ETTY);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ETML);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ARCH);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.PLAFO);
            query.AddOrderBy(K1PROFI.Name, K1PROFI.Fields.PFID, OrderDirection.ASC);

            List<T1PROFI> list = new List<T1PROFI>();
            using var reader = query.Execute();
            while (reader.Read())

            {
                T1PROFI obj = Map(reader);
                obj.AcceptChanges();
                list.Add(obj);
            }
            return list;
        }

        private void Insert(SQLiteConnection connection, T1PROFI obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();
            DateTime now = DateTime.Now;
            obj.CRAT = now;
            obj.CHAT = now;
            cmd.CommandText = "INSERT INTO T1PROFI (GANA, FIPL, LAPL, PPFN, EXGF, SAID, PRSE, EXEC, CRAT, CHAT, ACCO, ACIN, ACAC, CUPT, ETMA, ETME, ETCO, ETTY, ETML, ARCH, PLAFO) VALUES (@GANA, @FIPL, @LAPL, @PPFN, @EXGF, @SAID, @PRSE, @EXEC, @CRAT, @CHAT, @ACCO, @ACIN, @ACAC, @CUPT, @ETMA, @ETME, @ETCO, @ETTY, @ETML, @ARCH, @PLAFO)";
            cmd.Parameters.AddWithValue("@GANA", ToDbValue(obj.GANA));
            cmd.Parameters.AddWithValue("@FIPL", ToDbValue(obj.FIPL));
            cmd.Parameters.AddWithValue("@LAPL", ToDbValue(obj.LAPL));
            cmd.Parameters.AddWithValue("@PPFN", ToDbValue(obj.PPFN));
            cmd.Parameters.AddWithValue("@EXGF", ToDbValue(obj.EXGF));
            cmd.Parameters.AddWithValue("@SAID", ToDbValue(obj.SAID));
            cmd.Parameters.AddWithValue("@PRSE", ToDbValue(obj.PRSE));
            cmd.Parameters.AddWithValue("@EXEC", ToDbValue(obj.EXEC));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@ACCO", ToDbValue(obj.ACCO));
            cmd.Parameters.AddWithValue("@ACIN", ToDbValue(obj.ACIN));
            cmd.Parameters.AddWithValue("@ACAC", ToDbValue(obj.ACAC));
            cmd.Parameters.AddWithValue("@CUPT", ToDbValue(obj.CUPT));
            cmd.Parameters.AddWithValue("@ETMA", ToDbValue(obj.ETMA));
            cmd.Parameters.AddWithValue("@ETME", ToDbValue(obj.ETME));
            cmd.Parameters.AddWithValue("@ETCO", ToDbValue(obj.ETCO));
            cmd.Parameters.AddWithValue("@ETTY", ToDbValue(obj.ETTY));
            cmd.Parameters.AddWithValue("@ETML", ToDbValue(obj.ETML));
            cmd.Parameters.AddWithValue("@ARCH", ToDbValue(obj.ARCH));
            cmd.Parameters.AddWithValue("@PLAFO", ToDbValue(obj.PLAFO));
            cmd.ExecuteNonQuery();
            using SQLiteCommand idCmd = connection.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid();";
            obj.PFID = Convert.ToInt64(idCmd.ExecuteScalar());
        }

        private void Update(SQLiteConnection connection, T1PROFI obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();
            obj.CHAT = DateTime.Now;
            cmd.CommandText = "UPDATE T1PROFI SET GANA = @GANA, FIPL = @FIPL, LAPL = @LAPL, PPFN = @PPFN, EXGF = @EXGF, SAID = @SAID, PRSE = @PRSE, EXEC = @EXEC, CRAT = @CRAT, CHAT = @CHAT, ACCO = @ACCO, ACIN = @ACIN, ACAC = @ACAC, CUPT = @CUPT, ETMA = @ETMA, ETME = @ETME, ETCO = @ETCO, ETTY = @ETTY, ETML = @ETML, ARCH = @ARCH, PLAFO = @PLAFO WHERE PFID = @PFID";
            cmd.Parameters.AddWithValue("@PFID", ToDbValue(obj.PFID));
            cmd.Parameters.AddWithValue("@GANA", ToDbValue(obj.GANA));
            cmd.Parameters.AddWithValue("@FIPL", ToDbValue(obj.FIPL));
            cmd.Parameters.AddWithValue("@LAPL", ToDbValue(obj.LAPL));
            cmd.Parameters.AddWithValue("@PPFN", ToDbValue(obj.PPFN));
            cmd.Parameters.AddWithValue("@EXGF", ToDbValue(obj.EXGF));
            cmd.Parameters.AddWithValue("@SAID", ToDbValue(obj.SAID));
            cmd.Parameters.AddWithValue("@PRSE", ToDbValue(obj.PRSE));
            cmd.Parameters.AddWithValue("@EXEC", ToDbValue(obj.EXEC));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@ACCO", ToDbValue(obj.ACCO));
            cmd.Parameters.AddWithValue("@ACIN", ToDbValue(obj.ACIN));
            cmd.Parameters.AddWithValue("@ACAC", ToDbValue(obj.ACAC));
            cmd.Parameters.AddWithValue("@CUPT", ToDbValue(obj.CUPT));
            cmd.Parameters.AddWithValue("@ETMA", ToDbValue(obj.ETMA));
            cmd.Parameters.AddWithValue("@ETME", ToDbValue(obj.ETME));
            cmd.Parameters.AddWithValue("@ETCO", ToDbValue(obj.ETCO));
            cmd.Parameters.AddWithValue("@ETTY", ToDbValue(obj.ETTY));
            cmd.Parameters.AddWithValue("@ETML", ToDbValue(obj.ETML));
            cmd.Parameters.AddWithValue("@ARCH", ToDbValue(obj.ARCH));
            cmd.Parameters.AddWithValue("@PLAFO", ToDbValue(obj.PLAFO));
            cmd.ExecuteNonQuery();
        }

        private bool Exists(SQLiteConnection connection, T1PROFI obj)
        {
            using SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM T1PROFI WHERE PFID = @PFID";
            cmd.Parameters.AddWithValue("@PFID", ToDbValue(obj.PFID));
            return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        protected static T1PROFI Map(SQLiteDataReader reader)
        {
            T1PROFI obj = new T1PROFI();
            obj.PFID = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0));
            obj.GANA = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            obj.FIPL = ParseDbDateTime(reader.GetValue(2));
            obj.LAPL = ParseDbDateTime(reader.GetValue(3));
            obj.PPFN = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
            obj.EXGF = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);
            obj.SAID = reader.IsDBNull(6) ? 0 : Convert.ToInt64(reader.GetValue(6));
            obj.PRSE = reader.IsDBNull(7) ? string.Empty : reader.GetString(7);
            obj.EXEC = reader.IsDBNull(8) ? string.Empty : reader.GetString(8);
            obj.CRAT = ParseDbDateTime(reader.GetValue(9));
            obj.CHAT = ParseDbDateTime(reader.GetValue(10));
            obj.ACCO = reader.IsDBNull(11) ? string.Empty : reader.GetString(11);
            obj.ACIN = reader.IsDBNull(12) ? string.Empty : reader.GetString(12);
            obj.ACAC = !reader.IsDBNull(13) && Convert.ToInt32(reader.GetValue(13)) == 1;
            obj.CUPT = reader.IsDBNull(14) ? 0 : Convert.ToInt64(reader.GetValue(14));
            obj.ETMA = reader.IsDBNull(15) ? 0d : Convert.ToDouble(reader.GetValue(15), CultureInfo.InvariantCulture);
            obj.ETME = reader.IsDBNull(16) ? 0d : Convert.ToDouble(reader.GetValue(16), CultureInfo.InvariantCulture);
            obj.ETCO = reader.IsDBNull(17) ? 0d : Convert.ToDouble(reader.GetValue(17), CultureInfo.InvariantCulture);
            obj.ETTY = reader.IsDBNull(18) ? string.Empty : reader.GetString(18);
            obj.ETML = !reader.IsDBNull(19) && Convert.ToInt32(reader.GetValue(19)) == 1;
            obj.ARCH = !reader.IsDBNull(20) && Convert.ToInt32(reader.GetValue(20)) == 1;
            obj.PLAFO = reader.IsDBNull(21) ? string.Empty : reader.GetString(21);
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
