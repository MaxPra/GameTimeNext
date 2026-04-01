using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXPROFI
    {
        public T1PROFI CreateNew()
        {
            T1PROFI obj = new T1PROFI();

            DateTime now = DateTime.Now;
            obj.CRAT = now;
            obj.CHAT = now;

            obj.State = UIXTableObjectState.New;

            return obj;
        }

        public T1PROFI Copy(T1PROFI source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            T1PROFI copy = new T1PROFI();

            copy.PFID = 0;

            copy.GANA = source.GANA;
            copy.FIPL = source.FIPL;
            copy.LAPL = source.LAPL;

            copy.PPFN = source.PPFN;
            copy.EXGF = source.EXGF;

            copy.SAID = source.SAID;

            copy.PRSE = source.PRSE;
            copy.EXEC = source.EXEC;

            copy.ACCO = source.ACCO;
            copy.ACIN = source.ACIN;
            copy.ACAC = source.ACAC;
            copy.CUPT = source.CUPT;

            copy.ETMA = source.ETMA;
            copy.ETME = source.ETME;
            copy.ETCO = source.ETCO;
            copy.ETTY = source.ETTY;
            copy.ETML = source.ETML;
            copy.ARCH = source.ARCH;

            DateTime now = DateTime.Now;
            copy.CRAT = now;
            copy.CHAT = now;

            copy.State = UIXTableObjectState.New;

            return copy;
        }

        public void Save(T1PROFI obj)
        {
            Save(obj, false);
        }

        public void Save(T1PROFI obj, bool migration)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            DateTime now = DateTime.Now;

            if (obj.PFID <= 0 || migration)
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

        public void Delete(long pfid)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM T1PROFI WHERE PFID = @PFID;";
                cmd.Parameters.AddWithValue("@PFID", pfid);
                cmd.ExecuteNonQuery();
            }
        }

        private void Insert(SQLiteConnection connection, T1PROFI obj)
        {
            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "INSERT INTO T1PROFI " +
                    "(GANA, FIPL, LAPL, PPFN, EXGF, SAID, PRSE, EXEC, CRAT, CHAT, ACCO, ACIN, ACAC, CUPT, ETMA, ETME, ETCO, ETTY, ETML, ARCH) " +
                    "VALUES " +
                    "(@GANA, @FIPL, @LAPL, @PPFN, @EXGF, @SAID, @PRSE, @EXEC, @CRAT, @CHAT, @ACCO, @ACIN, @ACAC, @CUPT, @ETMA, @ETME, @ETCO, @ETTY, @ETML, @ARCH);";

                cmd.Parameters.AddWithValue("@GANA", obj.GANA);
                cmd.Parameters.AddWithValue("@FIPL", ToDbDateTime(obj.FIPL));
                cmd.Parameters.AddWithValue("@LAPL", ToDbDateTime(obj.LAPL));
                cmd.Parameters.AddWithValue("@PPFN", obj.PPFN);
                cmd.Parameters.AddWithValue("@EXGF", obj.EXGF);
                cmd.Parameters.AddWithValue("@SAID", obj.SAID);
                cmd.Parameters.AddWithValue("@PRSE", obj.PRSE);
                cmd.Parameters.AddWithValue("@EXEC", obj.EXEC);
                cmd.Parameters.AddWithValue("@CRAT", ToDbDateTime(obj.CRAT));
                cmd.Parameters.AddWithValue("@CHAT", ToDbDateTime(obj.CHAT));
                cmd.Parameters.AddWithValue("@ACCO", obj.ACCO);
                cmd.Parameters.AddWithValue("@ACIN", obj.ACIN);
                cmd.Parameters.AddWithValue("@ACAC", obj.ACAC ? 1 : 0);
                cmd.Parameters.AddWithValue("@CUPT", obj.CUPT);
                cmd.Parameters.AddWithValue("@ETMA", obj.ETMA);
                cmd.Parameters.AddWithValue("@ETME", obj.ETME);
                cmd.Parameters.AddWithValue("@ETCO", obj.ETCO);
                cmd.Parameters.AddWithValue("@ETTY", obj.ETTY);
                cmd.Parameters.AddWithValue("@ETML", obj.ETML ? 1 : 0);

                cmd.Parameters.AddWithValue("@ARCH", obj.ARCH ? 1 : 0);

                cmd.ExecuteNonQuery();
            }

            using (SQLiteCommand cmdId = connection.CreateCommand())
            {
                cmdId.CommandText = "SELECT last_insert_rowid();";
                object result = cmdId.ExecuteScalar();
                obj.PFID = Convert.ToInt64(result);
            }
        }

        private void Update(SQLiteConnection connection, T1PROFI obj)
        {
            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "UPDATE T1PROFI SET " +
                    "GANA = @GANA, " +
                    "FIPL = @FIPL, " +
                    "LAPL = @LAPL, " +
                    "PPFN = @PPFN, " +
                    "EXGF = @EXGF, " +
                    "SAID = @SAID, " +
                    "PRSE = @PRSE, " +
                    "EXEC = @EXEC, " +
                    "CRAT = @CRAT, " +
                    "CHAT = @CHAT, " +
                    "ACCO = @ACCO, " +
                    "ACIN = @ACIN, " +
                    "ACAC = @ACAC, " +
                    "CUPT = @CUPT, " +
                    "ETMA = @ETMA, " +
                    "ETME = @ETME, " +
                    "ETCO = @ETCO, " +
                    "ETTY = @ETTY, " +
                    "ETML = @ETML, " +
                    "ARCH = @ARCH " +
                    "WHERE PFID = @PFID;";

                cmd.Parameters.AddWithValue("@PFID", obj.PFID);
                cmd.Parameters.AddWithValue("@GANA", obj.GANA);
                cmd.Parameters.AddWithValue("@FIPL", ToDbDateTime(obj.FIPL));
                cmd.Parameters.AddWithValue("@LAPL", ToDbDateTime(obj.LAPL));
                cmd.Parameters.AddWithValue("@PPFN", obj.PPFN);
                cmd.Parameters.AddWithValue("@EXGF", obj.EXGF);
                cmd.Parameters.AddWithValue("@SAID", obj.SAID);
                cmd.Parameters.AddWithValue("@PRSE", obj.PRSE);
                cmd.Parameters.AddWithValue("@EXEC", obj.EXEC);
                cmd.Parameters.AddWithValue("@CRAT", ToDbDateTime(obj.CRAT));
                cmd.Parameters.AddWithValue("@CHAT", ToDbDateTime(obj.CHAT));
                cmd.Parameters.AddWithValue("@ACCO", obj.ACCO);
                cmd.Parameters.AddWithValue("@ACIN", obj.ACIN);
                cmd.Parameters.AddWithValue("@ACAC", obj.ACAC ? 1 : 0);
                cmd.Parameters.AddWithValue("@CUPT", obj.CUPT);
                cmd.Parameters.AddWithValue("@ETMA", obj.ETMA);
                cmd.Parameters.AddWithValue("@ETME", obj.ETME);
                cmd.Parameters.AddWithValue("@ETCO", obj.ETCO);
                cmd.Parameters.AddWithValue("@ETTY", obj.ETTY);
                cmd.Parameters.AddWithValue("@ETML", obj.ETML ? 1 : 0);
                cmd.Parameters.AddWithValue("@ARCH", obj.ARCH ? 1 : 0);

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

        public T1PROFI Read(long pfid)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT PFID, GANA, FIPL, LAPL, PPFN, EXGF, SAID, PRSE, EXEC, CRAT, CHAT, ACCO, ACIN, ACAC, CUPT, ETMA, ETME, ETCO, ETTY, ETML, ARCH " +
                    "FROM T1PROFI WHERE PFID = @PFID;";
                cmd.Parameters.AddWithValue("@PFID", pfid);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    T1PROFI obj = Map(reader);
                    obj.AcceptChanges();
                    return obj;
                }
            }
        }

        public List<T1PROFI> ReadAll()
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            List<T1PROFI> list = new List<T1PROFI>();

            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT PFID, GANA, FIPL, LAPL, PPFN, EXGF, SAID, PRSE, EXEC, CRAT, CHAT, ACCO, ACIN, ACAC, CUPT, ETMA, ETME, ETCO, ETTY, ETML, ARCH " +
                    "FROM T1PROFI ORDER BY PFID;";

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        T1PROFI obj = Map(reader);
                        obj.AcceptChanges();
                        list.Add(obj);
                    }
                }
            }

            return list;
        }

        private T1PROFI Map(SQLiteDataReader reader)
        {
            T1PROFI obj = new T1PROFI();

            obj.PFID = Convert.ToInt64(reader.GetValue(0));
            obj.GANA = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);

            obj.FIPL = ParseDbDateTime(reader.IsDBNull(2) ? null : reader.GetString(2));
            obj.LAPL = ParseDbDateTime(reader.IsDBNull(3) ? null : reader.GetString(3));

            obj.PPFN = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
            obj.EXGF = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);

            obj.SAID = reader.IsDBNull(6) ? 0 : Convert.ToInt64(reader.GetValue(6));

            obj.PRSE = reader.IsDBNull(7) ? string.Empty : reader.GetString(7);
            obj.EXEC = reader.IsDBNull(8) ? string.Empty : reader.GetString(8);

            obj.CRAT = ParseDbDateTime(reader.IsDBNull(9) ? null : reader.GetString(9));
            obj.CHAT = ParseDbDateTime(reader.IsDBNull(10) ? null : reader.GetString(10));

            obj.ACCO = reader.IsDBNull(11) ? string.Empty : reader.GetString(11);
            obj.ACIN = reader.IsDBNull(12) ? string.Empty : reader.GetString(12);
            obj.ACAC = !reader.IsDBNull(13) && (Convert.ToInt32(reader.GetValue(13)) == 1);
            obj.CUPT = reader.IsDBNull(14) ? 0 : Convert.ToInt64(reader.GetValue(14));
            obj.ETMA = reader.IsDBNull(15) ? 0 : Convert.ToDouble(reader.GetValue(15));
            obj.ETME = reader.IsDBNull(16) ? 0 : Convert.ToDouble(reader.GetValue(16));
            obj.ETCO = reader.IsDBNull(17) ? 0 : Convert.ToDouble(reader.GetValue(17));
            obj.ETTY = reader.IsDBNull(18) ? string.Empty : reader.GetString(18);
            obj.ETML = !reader.IsDBNull(19) && (Convert.ToInt32(reader.GetValue(19)) == 1);
            obj.ARCH = !reader.IsDBNull(20) && (Convert.ToInt32(reader.GetValue(20)) == 1);

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