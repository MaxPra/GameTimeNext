using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TBLM_PROFI
    {
        public TBL_PROFI CreateNew()
        {
            TBL_PROFI obj = new TBL_PROFI();

            DateTime now = DateTime.Now;
            obj.CRAT = now;
            obj.CHAT = now;

            obj.State = UIXTableObjectState.New;

            return obj;
        }

        public TBL_PROFI Copy(TBL_PROFI source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            TBL_PROFI copy = new TBL_PROFI();

            copy.PFID = 0;

            copy.GANA = source.GANA;
            copy.FIPL = source.FIPL;
            copy.LAPL = source.LAPL;

            copy.PPFN = source.PPFN;
            copy.EXGF = source.EXGF;

            copy.SAID = source.SAID;

            copy.PRSE = source.PRSE;
            copy.EXEC = source.EXEC;

            copy.PLSP = source.PLSP;
            copy.ACCO = source.ACCO;

            DateTime now = DateTime.Now;
            copy.CRAT = now;
            copy.CHAT = now;

            copy.State = UIXTableObjectState.New;

            return copy;
        }

        public void Save(TBL_PROFI obj)
        {
            Save(obj, false);
        }

        public void Save(TBL_PROFI obj, bool migration)
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
                cmd.CommandText = "DELETE FROM TBL_PROFI WHERE PFID = @PFID;";
                cmd.Parameters.AddWithValue("@PFID", pfid);
                cmd.ExecuteNonQuery();
            }
        }

        private void Insert(SQLiteConnection connection, TBL_PROFI obj)
        {
            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "INSERT INTO TBL_PROFI " +
                    "(GANA, FIPL, LAPL, PPFN, EXGF, SAID, PRSE, EXEC, PLSP, CRAT, CHAT, ACCO) " +
                    "VALUES " +
                    "(@GANA, @FIPL, @LAPL, @PPFN, @EXGF, @SAID, @PRSE, @EXEC, @PLSP, @CRAT, @CHAT, @ACCO);";

                cmd.Parameters.AddWithValue("@GANA", obj.GANA);
                cmd.Parameters.AddWithValue("@FIPL", ToDbDateTime(obj.FIPL));
                cmd.Parameters.AddWithValue("@LAPL", ToDbDateTime(obj.LAPL));
                cmd.Parameters.AddWithValue("@PPFN", obj.PPFN);
                cmd.Parameters.AddWithValue("@EXGF", obj.EXGF);
                cmd.Parameters.AddWithValue("@SAID", obj.SAID);
                cmd.Parameters.AddWithValue("@PRSE", obj.PRSE);
                cmd.Parameters.AddWithValue("@EXEC", obj.EXEC);
                cmd.Parameters.AddWithValue("@PLSP", ToDbDateTime(obj.PLSP));
                cmd.Parameters.AddWithValue("@CRAT", ToDbDateTime(obj.CRAT));
                cmd.Parameters.AddWithValue("@CHAT", ToDbDateTime(obj.CHAT));
                cmd.Parameters.AddWithValue("@ACCO", obj.ACCO);

                cmd.ExecuteNonQuery();
            }

            using (SQLiteCommand cmdId = connection.CreateCommand())
            {
                cmdId.CommandText = "SELECT last_insert_rowid();";
                object result = cmdId.ExecuteScalar();
                obj.PFID = Convert.ToInt64(result);
            }
        }

        private void Update(SQLiteConnection connection, TBL_PROFI obj)
        {
            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "UPDATE TBL_PROFI SET " +
                    "GANA = @GANA, " +
                    "FIPL = @FIPL, " +
                    "LAPL = @LAPL, " +
                    "PPFN = @PPFN, " +
                    "EXGF = @EXGF, " +
                    "SAID = @SAID, " +
                    "PRSE = @PRSE, " +
                    "EXEC = @EXEC, " +
                    "PLSP = @PLSP, " +
                    "CRAT = @CRAT, " +
                    "CHAT = @CHAT, " +
                    "ACCO = @ACCO " +
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
                cmd.Parameters.AddWithValue("@PLSP", ToDbDateTime(obj.PLSP));
                cmd.Parameters.AddWithValue("@CRAT", ToDbDateTime(obj.CRAT));
                cmd.Parameters.AddWithValue("@CHAT", ToDbDateTime(obj.CHAT));
                cmd.Parameters.AddWithValue("@ACCO", obj.ACCO);

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

        public TBL_PROFI Read(long pfid)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT PFID, GANA, FIPL, LAPL, PPFN, EXGF, SAID, PRSE, EXEC, PLSP, CRAT, CHAT, ACCO " +
                    "FROM TBL_PROFI WHERE PFID = @PFID;";
                cmd.Parameters.AddWithValue("@PFID", pfid);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    TBL_PROFI obj = Map(reader);
                    obj.AcceptChanges();
                    return obj;
                }
            }
        }

        public List<TBL_PROFI> ReadAll()
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            List<TBL_PROFI> list = new List<TBL_PROFI>();

            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT PFID, GANA, FIPL, LAPL, PPFN, EXGF, SAID, PRSE, EXEC, PLSP, CRAT, CHAT, ACCO " +
                    "FROM TBL_PROFI ORDER BY PFID;";

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        TBL_PROFI obj = Map(reader);
                        obj.AcceptChanges();
                        list.Add(obj);
                    }
                }
            }

            return list;
        }

        private TBL_PROFI Map(SQLiteDataReader reader)
        {
            TBL_PROFI obj = new TBL_PROFI();

            obj.PFID = Convert.ToInt64(reader.GetValue(0));
            obj.GANA = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);

            obj.FIPL = ParseDbDateTime(reader.IsDBNull(2) ? null : reader.GetString(2));
            obj.LAPL = ParseDbDateTime(reader.IsDBNull(3) ? null : reader.GetString(3));

            obj.PPFN = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
            obj.EXGF = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);

            obj.SAID = reader.IsDBNull(6) ? 0 : Convert.ToInt64(reader.GetValue(6));

            obj.PRSE = reader.IsDBNull(7) ? string.Empty : reader.GetString(7);
            obj.EXEC = reader.IsDBNull(8) ? string.Empty : reader.GetString(8);

            obj.PLSP = ParseDbDateTime(reader.IsDBNull(9) ? null : reader.GetString(9));

            obj.CRAT = ParseDbDateTime(reader.IsDBNull(10) ? null : reader.GetString(10));
            obj.CHAT = ParseDbDateTime(reader.IsDBNull(11) ? null : reader.GetString(11));

            obj.ACCO = reader.IsDBNull(12) ? string.Empty : reader.GetString(12);

            obj.State = UIXTableObjectState.Available;

            return obj;
        }

        private DateTime ParseDbDateTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DateTime.MinValue;
            }

            if (DateTime.TryParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
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