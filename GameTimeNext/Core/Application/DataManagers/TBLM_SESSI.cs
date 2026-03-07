using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.DataManagers
{
    internal class TBLM_SESSI
    {
        public TBL_SESSI CreateNew()
        {
            TBL_SESSI obj = new TBL_SESSI();

            DateTime now = DateTime.Now;
            obj.CRAT = now;
            obj.CHAT = now;

            obj.State = UIXTableObjectState.New;

            return obj;
        }

        public TBL_SESSI Copy(TBL_SESSI source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            TBL_SESSI copy = new TBL_SESSI();

            copy.SEID = 0;

            copy.PFID = source.PFID;

            copy.PLFR = source.PLFR;
            copy.PLTO = source.PLTO;
            copy.PLTI = source.PLTI;

            DateTime now = DateTime.Now;
            copy.CRAT = now;
            copy.CHAT = now;

            copy.State = UIXTableObjectState.New;

            return copy;
        }

        public void Save(TBL_SESSI obj)
        {
            Save(obj, false);
        }

        public void Save(TBL_SESSI obj, bool migration)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            if (obj.PFID <= 0)
                throw new InvalidOperationException("PFID muss > 0 sein, da TBL_SESSI auf ein Profil referenziert.");

            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            DateTime now = DateTime.Now;

            if (obj.SEID <= 0 || migration)
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

        public void Delete(long seid)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM TBL_SESSI WHERE SEID = @SEID;";
                cmd.Parameters.AddWithValue("@SEID", seid);
                cmd.ExecuteNonQuery();
            }
        }

        public TBL_SESSI Read(long seid)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT SEID, PFID, PLFR, PLTO, PLTI, CRAT, CHAT " +
                    "FROM TBL_SESSI WHERE SEID = @SEID;";
                cmd.Parameters.AddWithValue("@SEID", seid);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    TBL_SESSI obj = Map(reader);
                    obj.AcceptChanges();
                    return obj;
                }
            }
        }

        public List<TBL_SESSI> ReadAll()
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            List<TBL_SESSI> list = new List<TBL_SESSI>();

            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT SEID, PFID, PLFR, PLTO, PLTI, CRAT, CHAT " +
                    "FROM TBL_SESSI ORDER BY SEID;";

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        TBL_SESSI obj = Map(reader);
                        obj.AcceptChanges();
                        list.Add(obj);
                    }
                }
            }

            return list;
        }

        private void Insert(SQLiteConnection connection, TBL_SESSI obj)
        {
            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "INSERT INTO TBL_SESSI (PFID, PLFR, PLTO, PLTI, CRAT, CHAT) " +
                    "VALUES (@PFID, @PLFR, @PLTO, @PLTI, @CRAT, @CHAT);";

                cmd.Parameters.AddWithValue("@PFID", obj.PFID);
                cmd.Parameters.AddWithValue("@PLFR", ToDbDateTime(obj.PLFR));
                cmd.Parameters.AddWithValue("@PLTO", ToDbDateTime(obj.PLTO));
                cmd.Parameters.AddWithValue("@PLTI", obj.PLTI);
                cmd.Parameters.AddWithValue("@CRAT", ToDbDateTime(obj.CRAT));
                cmd.Parameters.AddWithValue("@CHAT", ToDbDateTime(obj.CHAT));

                cmd.ExecuteNonQuery();
            }

            using (SQLiteCommand cmdId = connection.CreateCommand())
            {
                cmdId.CommandText = "SELECT last_insert_rowid();";
                object result = cmdId.ExecuteScalar();
                obj.SEID = Convert.ToInt64(result);
            }
        }

        private void Update(SQLiteConnection connection, TBL_SESSI obj)
        {
            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "UPDATE TBL_SESSI SET " +
                    "PFID = @PFID, " +
                    "PLFR = @PLFR, " +
                    "PLTO = @PLTO, " +
                    "PLTI = @PLTI, " +
                    "CRAT = @CRAT, " +
                    "CHAT = @CHAT " +
                    "WHERE SEID = @SEID;";

                cmd.Parameters.AddWithValue("@SEID", obj.SEID);

                cmd.Parameters.AddWithValue("@PFID", obj.PFID);
                cmd.Parameters.AddWithValue("@PLFR", ToDbDateTime(obj.PLFR));
                cmd.Parameters.AddWithValue("@PLTO", ToDbDateTime(obj.PLTO));
                cmd.Parameters.AddWithValue("@PLTI", obj.PLTI);
                cmd.Parameters.AddWithValue("@CRAT", ToDbDateTime(obj.CRAT));
                cmd.Parameters.AddWithValue("@CHAT", ToDbDateTime(obj.CHAT));

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

        private TBL_SESSI Map(SQLiteDataReader reader)
        {
            TBL_SESSI obj = new TBL_SESSI();

            obj.SEID = Convert.ToInt64(reader.GetValue(0));
            obj.PFID = Convert.ToInt64(reader.GetValue(1));

            obj.PLFR = ParseDbDateTime(reader.IsDBNull(2) ? null : reader.GetString(2));
            obj.PLTO = ParseDbDateTime(reader.IsDBNull(3) ? null : reader.GetString(3));

            obj.PLTI = reader.IsDBNull(4) ? 0.0 : Convert.ToDouble(reader.GetValue(4));

            obj.CRAT = ParseDbDateTime(reader.IsDBNull(5) ? null : reader.GetString(5));
            obj.CHAT = ParseDbDateTime(reader.IsDBNull(6) ? null : reader.GetString(6));

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