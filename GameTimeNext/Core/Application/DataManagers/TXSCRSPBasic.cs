using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase.Migration;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Migration.Types;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXSCRSPBasic
    {
        private readonly SQLiteConnection? _connectionOverride;

        protected SQLiteConnection _connection
        {
            get => _connectionOverride ?? AppEnvironment.GetDataBaseManager().GetConnection();
        }

        public TXSCRSPBasic()
        {
        }

        public TXSCRSPBasic(SQLiteConnection connection)
        {
            _connectionOverride = connection;
        }

        #region Methods PUBLIC
        public virtual T1SCRSP CreateNew()
        {
            T1SCRSP obj = new T1SCRSP();
            obj.State = UIXTableObjectState.New;
            return obj;
        }

        public virtual void Save(T1SCRSP obj)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (Exists(obj))
            {
                Update(obj);
            }
            else
            {
                Insert(obj);
            }
            obj.State = UIXTableObjectState.Available;
            obj.AcceptChanges();
            MigrationFactory.ToCsv.ExportCsvFileFor(_connection, obj, MigrationFactory.ImportType.DevSync);
        }

        public virtual void Delete(long spid)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM T1SCRSP WHERE SPID = @SPID;";
            cmd.Parameters.AddWithValue("@SPID", ToDbValue(spid));
            cmd.ExecuteNonQuery();
            MigrationFactory.ToCsv.ExportCsvFileFor(_connection, "T1SCRSP", MigrationFactory.ImportType.DevSync);
        }

        public bool Exists(T1SCRSP obj)
        {
            return Exists(obj.SPID);
        }

        public bool Exists(long spid)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM T1SCRSP WHERE SPID = @SPID;";
            cmd.Parameters.AddWithValue("@SPID", ToDbValue(spid));
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        public virtual T1SCRSP? Read(long spid)
        {
            UIXQuery query = new UIXQuery(K1SCRSP.Name, _connection);
            query.AddField(K1SCRSP.Name, K1SCRSP.Fields.SPID);
            query.AddField(K1SCRSP.Name, K1SCRSP.Fields.SSID);
            query.AddField(K1SCRSP.Name, K1SCRSP.Fields.MOID);
            query.AddField(K1SCRSP.Name, K1SCRSP.Fields.MONA);
            query.AddField(K1SCRSP.Name, K1SCRSP.Fields.MOEN);
            query.AddField(K1SCRSP.Name, K1SCRSP.Fields.MOPR);
            query.AddField(K1SCRSP.Name, K1SCRSP.Fields.CRAT);
            query.AddField(K1SCRSP.Name, K1SCRSP.Fields.CRUS);
            query.AddField(K1SCRSP.Name, K1SCRSP.Fields.CHAT);
            query.AddField(K1SCRSP.Name, K1SCRSP.Fields.CHUS);
            query.AddWhere(K1SCRSP.Name, K1SCRSP.Fields.SPID, QueryCompareType.EQUALS, spid);

            using var reader = query.Execute();
            if (!reader.Read())
            {
                return null;
            }

            T1SCRSP obj = Map(reader);
            obj.AcceptChanges();
            return obj;
        }

        public virtual List<T1SCRSP> ReadAll()
        {
            UIXQuery query = new UIXQuery(K1SCRSP.Name, _connection);
            query.AddField(K1SCRSP.Name, K1SCRSP.Fields.SPID);
            query.AddField(K1SCRSP.Name, K1SCRSP.Fields.SSID);
            query.AddField(K1SCRSP.Name, K1SCRSP.Fields.MOID);
            query.AddField(K1SCRSP.Name, K1SCRSP.Fields.MONA);
            query.AddField(K1SCRSP.Name, K1SCRSP.Fields.MOEN);
            query.AddField(K1SCRSP.Name, K1SCRSP.Fields.MOPR);
            query.AddField(K1SCRSP.Name, K1SCRSP.Fields.CRAT);
            query.AddField(K1SCRSP.Name, K1SCRSP.Fields.CRUS);
            query.AddField(K1SCRSP.Name, K1SCRSP.Fields.CHAT);
            query.AddField(K1SCRSP.Name, K1SCRSP.Fields.CHUS);
            query.AddOrderBy(K1SCRSP.Name, K1SCRSP.Fields.SPID, OrderDirection.ASC);

            List<T1SCRSP> list = new List<T1SCRSP>();
            using var reader = query.Execute();
            while (reader.Read())
            {
                T1SCRSP obj = Map(reader);
                obj.AcceptChanges();
                list.Add(obj);
            }
            return list;
        }
        #endregion

        #region Methods PROTECTED

        protected static T1SCRSP Map(SQLiteDataReader reader)
        {
            T1SCRSP obj = new T1SCRSP();
            obj.SPID = UIXQuery.GetInt64(reader, K1SCRSP.Name, K1SCRSP.Fields.SPID);
            obj.SSID = UIXQuery.GetInt64(reader, K1SCRSP.Name, K1SCRSP.Fields.SSID);
            obj.MOID = UIXQuery.GetString(reader, K1SCRSP.Name, K1SCRSP.Fields.MOID, def: "");
            obj.MONA = UIXQuery.GetString(reader, K1SCRSP.Name, K1SCRSP.Fields.MONA, def: "");
            obj.MOEN = UIXQuery.GetBool(reader, K1SCRSP.Name, K1SCRSP.Fields.MOEN);
            obj.MOPR = UIXQuery.GetBool(reader, K1SCRSP.Name, K1SCRSP.Fields.MOPR);
            obj.CRAT = UIXQuery.GetDateTime(reader, K1SCRSP.Name, K1SCRSP.Fields.CRAT);
            obj.CRUS = UIXQuery.GetString(reader, K1SCRSP.Name, K1SCRSP.Fields.CRUS, def: "");
            obj.CHAT = UIXQuery.GetDateTime(reader, K1SCRSP.Name, K1SCRSP.Fields.CHAT);
            obj.CHUS = UIXQuery.GetString(reader, K1SCRSP.Name, K1SCRSP.Fields.CHUS, def: "");
            obj.State = UIXTableObjectState.Available;
            return obj;
        }
        #endregion

        #region Methods PRIVATE
        private void Insert(T1SCRSP obj)
        {
            obj.CRAT = DateTime.Now;
            obj.CHAT = DateTime.Now;
            obj.CRUS = FnOpSys.GetCurrentUserName();
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO T1SCRSP (SSID, MOID, MONA, MOEN, MOPR, CRAT, CRUS, CHAT, CHUS) VALUES (@SSID, @MOID, @MONA, @MOEN, @MOPR, @CRAT, @CRUS, @CHAT, @CHUS);";
            cmd.Parameters.AddWithValue("@SSID", ToDbValue(obj.SSID));
            cmd.Parameters.AddWithValue("@MOID", ToDbValue(obj.MOID));
            cmd.Parameters.AddWithValue("@MONA", ToDbValue(obj.MONA));
            cmd.Parameters.AddWithValue("@MOEN", ToDbValue(obj.MOEN));
            cmd.Parameters.AddWithValue("@MOPR", ToDbValue(obj.MOPR));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CRUS", ToDbValue(obj.CRUS));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@CHUS", ToDbValue(obj.CHUS));
            cmd.ExecuteNonQuery();

            using SQLiteCommand idCmd = _connection.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid();";
            obj.SPID = Convert.ToInt64(idCmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private void Update(T1SCRSP obj)
        {
            obj.CHAT = DateTime.Now;
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE T1SCRSP SET SSID = @SSID, MOID = @MOID, MONA = @MONA, MOEN = @MOEN, MOPR = @MOPR, CRAT = @CRAT, CRUS = @CRUS, CHAT = @CHAT, CHUS = @CHUS WHERE SPID = @SPID;";
            cmd.Parameters.AddWithValue("@SPID", ToDbValue(obj.SPID));
            cmd.Parameters.AddWithValue("@SSID", ToDbValue(obj.SSID));
            cmd.Parameters.AddWithValue("@MOID", ToDbValue(obj.MOID));
            cmd.Parameters.AddWithValue("@MONA", ToDbValue(obj.MONA));
            cmd.Parameters.AddWithValue("@MOEN", ToDbValue(obj.MOEN));
            cmd.Parameters.AddWithValue("@MOPR", ToDbValue(obj.MOPR));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CRUS", ToDbValue(obj.CRUS));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@CHUS", ToDbValue(obj.CHUS));
            cmd.ExecuteNonQuery();
        }

        private static object? ToDbValue(object? valFrom)
        {
            return UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.CSharp, UIXSqliteDataType.ConversionType.Sqlite, valFrom);
        }
        #endregion
    }
}
