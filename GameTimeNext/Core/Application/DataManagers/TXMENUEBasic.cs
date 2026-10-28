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
    public class TXMENUEBasic
    {
        private readonly SQLiteConnection? _connectionOverride;

        protected SQLiteConnection _connection
        {
            get => _connectionOverride ?? AppEnvironment.GetDataBaseManager().GetConnection();
        }

        public TXMENUEBasic()
        {
        }

        public TXMENUEBasic(SQLiteConnection connection)
        {
            _connectionOverride = connection;
        }

        #region Methods PUBLIC
        public virtual T1MENUE CreateNew()
        {
            T1MENUE obj = new T1MENUE();
            obj.State = UIXTableObjectState.New;
            return obj;
        }

        public virtual void Save(T1MENUE obj)
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

        public virtual void Delete(long meid)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM T1MENUE WHERE MEID = @MEID;";
            cmd.Parameters.AddWithValue("@MEID", ToDbValue(meid));
            cmd.ExecuteNonQuery();
            MigrationFactory.ToCsv.ExportCsvFileFor(_connection, "T1MENUE", MigrationFactory.ImportType.DevSync);
        }

        public bool Exists(T1MENUE obj)
        {
            return Exists(obj.MEID);
        }

        public bool Exists(long meid)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM T1MENUE WHERE MEID = @MEID;";
            cmd.Parameters.AddWithValue("@MEID", ToDbValue(meid));
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        public virtual T1MENUE? Read(long meid)
        {
            UIXQuery query = new UIXQuery(K1MENUE.Name, _connection);
            query.AddField(K1MENUE.Name, K1MENUE.Fields.MEID);
            query.AddField(K1MENUE.Name, K1MENUE.Fields.MEPA);
            query.AddField(K1MENUE.Name, K1MENUE.Fields.METY);
            query.AddField(K1MENUE.Name, K1MENUE.Fields.MEDE);
            query.AddField(K1MENUE.Name, K1MENUE.Fields.MEAP);
            query.AddField(K1MENUE.Name, K1MENUE.Fields.DEVT);
            query.AddField(K1MENUE.Name, K1MENUE.Fields.MEOR);
            query.AddField(K1MENUE.Name, K1MENUE.Fields.CRAT);
            query.AddField(K1MENUE.Name, K1MENUE.Fields.CRUS);
            query.AddField(K1MENUE.Name, K1MENUE.Fields.CHAT);
            query.AddField(K1MENUE.Name, K1MENUE.Fields.CHUS);
            query.AddWhere(K1MENUE.Name, K1MENUE.Fields.MEID, QueryCompareType.EQUALS, meid);

            using var reader = query.Execute();
            if (!reader.Read())
            {
                return null;
            }

            T1MENUE obj = Map(reader);
            obj.AcceptChanges();
            return obj;
        }

        public virtual List<T1MENUE> ReadAll()
        {
            UIXQuery query = new UIXQuery(K1MENUE.Name, _connection);
            query.AddField(K1MENUE.Name, K1MENUE.Fields.MEID);
            query.AddField(K1MENUE.Name, K1MENUE.Fields.MEPA);
            query.AddField(K1MENUE.Name, K1MENUE.Fields.METY);
            query.AddField(K1MENUE.Name, K1MENUE.Fields.MEDE);
            query.AddField(K1MENUE.Name, K1MENUE.Fields.MEAP);
            query.AddField(K1MENUE.Name, K1MENUE.Fields.DEVT);
            query.AddField(K1MENUE.Name, K1MENUE.Fields.MEOR);
            query.AddField(K1MENUE.Name, K1MENUE.Fields.CRAT);
            query.AddField(K1MENUE.Name, K1MENUE.Fields.CRUS);
            query.AddField(K1MENUE.Name, K1MENUE.Fields.CHAT);
            query.AddField(K1MENUE.Name, K1MENUE.Fields.CHUS);
            query.AddOrderBy(K1MENUE.Name, K1MENUE.Fields.MEID, OrderDirection.ASC);

            List<T1MENUE> list = new List<T1MENUE>();
            using var reader = query.Execute();
            while (reader.Read())
            {
                T1MENUE obj = Map(reader);
                obj.AcceptChanges();
                list.Add(obj);
            }
            return list;
        }
        #endregion

        #region Methods PROTECTED

        protected static T1MENUE Map(SQLiteDataReader reader)
        {
            T1MENUE obj = new T1MENUE();
            obj.MEID = UIXQuery.GetInt64(reader, K1MENUE.Name, K1MENUE.Fields.MEID);
            obj.MEPA = UIXQuery.GetInt64(reader, K1MENUE.Name, K1MENUE.Fields.MEPA);
            obj.METY = UIXQuery.GetString(reader, K1MENUE.Name, K1MENUE.Fields.METY, def: "");
            obj.MEDE = UIXQuery.GetString(reader, K1MENUE.Name, K1MENUE.Fields.MEDE, def: "");
            obj.MEAP = UIXQuery.GetString(reader, K1MENUE.Name, K1MENUE.Fields.MEAP, def: "");
            obj.DEVT = UIXQuery.GetBool(reader, K1MENUE.Name, K1MENUE.Fields.DEVT);
            obj.MEOR = UIXQuery.GetInt64(reader, K1MENUE.Name, K1MENUE.Fields.MEOR);
            obj.CRAT = UIXQuery.GetDateTime(reader, K1MENUE.Name, K1MENUE.Fields.CRAT);
            obj.CRUS = UIXQuery.GetString(reader, K1MENUE.Name, K1MENUE.Fields.CRUS, def: "");
            obj.CHAT = UIXQuery.GetDateTime(reader, K1MENUE.Name, K1MENUE.Fields.CHAT);
            obj.CHUS = UIXQuery.GetString(reader, K1MENUE.Name, K1MENUE.Fields.CHUS, def: "");
            obj.State = UIXTableObjectState.Available;
            return obj;
        }
        #endregion

        #region Methods PRIVATE
        private void Insert(T1MENUE obj)
        {
            obj.CRAT = DateTime.Now;
            obj.CHAT = DateTime.Now;
            obj.CRUS = FnOpSys.GetCurrentUserName();
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO T1MENUE (MEPA, METY, MEDE, MEAP, DEVT, MEOR, CRAT, CRUS, CHAT, CHUS) VALUES (@MEPA, @METY, @MEDE, @MEAP, @DEVT, @MEOR, @CRAT, @CRUS, @CHAT, @CHUS);";
            cmd.Parameters.AddWithValue("@MEPA", ToDbValue(obj.MEPA));
            cmd.Parameters.AddWithValue("@METY", ToDbValue(obj.METY));
            cmd.Parameters.AddWithValue("@MEDE", ToDbValue(obj.MEDE));
            cmd.Parameters.AddWithValue("@MEAP", ToDbValue(obj.MEAP));
            cmd.Parameters.AddWithValue("@DEVT", ToDbValue(obj.DEVT));
            cmd.Parameters.AddWithValue("@MEOR", ToDbValue(obj.MEOR));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CRUS", ToDbValue(obj.CRUS));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@CHUS", ToDbValue(obj.CHUS));
            cmd.ExecuteNonQuery();

            using SQLiteCommand idCmd = _connection.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid();";
            obj.MEID = Convert.ToInt64(idCmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private void Update(T1MENUE obj)
        {
            obj.CHAT = DateTime.Now;
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE T1MENUE SET MEPA = @MEPA, METY = @METY, MEDE = @MEDE, MEAP = @MEAP, DEVT = @DEVT, MEOR = @MEOR, CRAT = @CRAT, CRUS = @CRUS, CHAT = @CHAT, CHUS = @CHUS WHERE MEID = @MEID;";
            cmd.Parameters.AddWithValue("@MEID", ToDbValue(obj.MEID));
            cmd.Parameters.AddWithValue("@MEPA", ToDbValue(obj.MEPA));
            cmd.Parameters.AddWithValue("@METY", ToDbValue(obj.METY));
            cmd.Parameters.AddWithValue("@MEDE", ToDbValue(obj.MEDE));
            cmd.Parameters.AddWithValue("@MEAP", ToDbValue(obj.MEAP));
            cmd.Parameters.AddWithValue("@DEVT", ToDbValue(obj.DEVT));
            cmd.Parameters.AddWithValue("@MEOR", ToDbValue(obj.MEOR));
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
