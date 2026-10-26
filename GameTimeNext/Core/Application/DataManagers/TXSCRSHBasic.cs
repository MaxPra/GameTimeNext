using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase.Migration;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Migration.Types;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXSCRSHBasic
    {
        private readonly SQLiteConnection? _connectionOverride;

        protected SQLiteConnection _connection
        {
            get => _connectionOverride ?? AppEnvironment.GetDataBaseManager().GetConnection();
        }

        public TXSCRSHBasic()
        {
        }

        public TXSCRSHBasic(SQLiteConnection connection)
        {
            _connectionOverride = connection;
        }

        #region Methods PUBLIC
        public virtual T1SCRSH CreateNew()
        {
            T1SCRSH obj = new T1SCRSH();
            obj.State = UIXTableObjectState.New;
            return obj;
        }

        public virtual void Save(T1SCRSH obj)
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

        public virtual void Delete(long ssid)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM T1SCRSH WHERE SSID = @SSID;";
            cmd.Parameters.AddWithValue("@SSID", ToDbValue(ssid));
            cmd.ExecuteNonQuery();
            MigrationFactory.ToCsv.ExportCsvFileFor(_connection, "T1SCRSH", MigrationFactory.ImportType.DevSync);
        }

        public bool Exists(T1SCRSH obj)
        {
            return Exists(obj.SSID);
        }

        public bool Exists(long ssid)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM T1SCRSH WHERE SSID = @SSID;";
            cmd.Parameters.AddWithValue("@SSID", ToDbValue(ssid));
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        public virtual T1SCRSH? Read(long ssid)
        {
            UIXQuery query = new UIXQuery(K1SCRSH.Name, _connection);
            query.AddField(K1SCRSH.Name, K1SCRSH.Fields.SSID);
            query.AddField(K1SCRSH.Name, K1SCRSH.Fields.SSNA);
            query.AddField(K1SCRSH.Name, K1SCRSH.Fields.SSDE);
            query.AddField(K1SCRSH.Name, K1SCRSH.Fields.ADID);
            query.AddField(K1SCRSH.Name, K1SCRSH.Fields.ADNA);
            query.AddField(K1SCRSH.Name, K1SCRSH.Fields.CRAT);
            query.AddField(K1SCRSH.Name, K1SCRSH.Fields.CHAT);
            query.AddWhere(K1SCRSH.Name, K1SCRSH.Fields.SSID, QueryCompareType.EQUALS, ssid);

            using var reader = query.Execute();
            if (!reader.Read())
            {
                return null;
            }

            T1SCRSH obj = Map(reader);
            obj.AcceptChanges();
            return obj;
        }

        public virtual List<T1SCRSH> ReadAll()
        {
            UIXQuery query = new UIXQuery(K1SCRSH.Name, _connection);
            query.AddField(K1SCRSH.Name, K1SCRSH.Fields.SSID);
            query.AddField(K1SCRSH.Name, K1SCRSH.Fields.SSNA);
            query.AddField(K1SCRSH.Name, K1SCRSH.Fields.SSDE);
            query.AddField(K1SCRSH.Name, K1SCRSH.Fields.ADID);
            query.AddField(K1SCRSH.Name, K1SCRSH.Fields.ADNA);
            query.AddField(K1SCRSH.Name, K1SCRSH.Fields.CRAT);
            query.AddField(K1SCRSH.Name, K1SCRSH.Fields.CHAT);
            query.AddOrderBy(K1SCRSH.Name, K1SCRSH.Fields.SSID, OrderDirection.ASC);

            List<T1SCRSH> list = new List<T1SCRSH>();
            using var reader = query.Execute();
            while (reader.Read())
            {
                T1SCRSH obj = Map(reader);
                obj.AcceptChanges();
                list.Add(obj);
            }
            return list;
        }
        #endregion

        #region Methods PROTECTED

        protected static T1SCRSH Map(SQLiteDataReader reader)
        {
            T1SCRSH obj = new T1SCRSH();
            obj.SSID = UIXQuery.GetInt64(reader, K1SCRSH.Name, K1SCRSH.Fields.SSID);
            obj.SSNA = UIXQuery.GetString(reader, K1SCRSH.Name, K1SCRSH.Fields.SSNA, def: "");
            obj.SSDE = UIXQuery.GetString(reader, K1SCRSH.Name, K1SCRSH.Fields.SSDE, def: "");
            obj.ADID = UIXQuery.GetString(reader, K1SCRSH.Name, K1SCRSH.Fields.ADID, def: "");
            obj.ADNA = UIXQuery.GetString(reader, K1SCRSH.Name, K1SCRSH.Fields.ADNA, def: "");
            obj.CRAT = UIXQuery.GetDateTime(reader, K1SCRSH.Name, K1SCRSH.Fields.CRAT);
            obj.CHAT = UIXQuery.GetDateTime(reader, K1SCRSH.Name, K1SCRSH.Fields.CHAT);
            obj.State = UIXTableObjectState.Available;
            return obj;
        }
        #endregion

        #region Methods PRIVATE
        private void Insert(T1SCRSH obj)
        {
            obj.CRAT = DateTime.Now;
            obj.CHAT = DateTime.Now;
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO T1SCRSH (SSNA, SSDE, ADID, ADNA, CRAT, CHAT) VALUES (@SSNA, @SSDE, @ADID, @ADNA, @CRAT, @CHAT);";
            cmd.Parameters.AddWithValue("@SSNA", ToDbValue(obj.SSNA));
            cmd.Parameters.AddWithValue("@SSDE", ToDbValue(obj.SSDE));
            cmd.Parameters.AddWithValue("@ADID", ToDbValue(obj.ADID));
            cmd.Parameters.AddWithValue("@ADNA", ToDbValue(obj.ADNA));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.ExecuteNonQuery();

            using SQLiteCommand idCmd = _connection.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid();";
            obj.SSID = Convert.ToInt64(idCmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private void Update(T1SCRSH obj)
        {
            obj.CHAT = DateTime.Now;
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE T1SCRSH SET SSNA = @SSNA, SSDE = @SSDE, ADID = @ADID, ADNA = @ADNA, CRAT = @CRAT, CHAT = @CHAT WHERE SSID = @SSID;";
            cmd.Parameters.AddWithValue("@SSID", ToDbValue(obj.SSID));
            cmd.Parameters.AddWithValue("@SSNA", ToDbValue(obj.SSNA));
            cmd.Parameters.AddWithValue("@SSDE", ToDbValue(obj.SSDE));
            cmd.Parameters.AddWithValue("@ADID", ToDbValue(obj.ADID));
            cmd.Parameters.AddWithValue("@ADNA", ToDbValue(obj.ADNA));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.ExecuteNonQuery();
        }

        private static object? ToDbValue(object? valFrom)
        {
            return UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.CSharp, UIXSqliteDataType.ConversionType.Sqlite, valFrom);
        }
        #endregion
    }
}
