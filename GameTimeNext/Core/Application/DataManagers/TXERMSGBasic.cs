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
    public class TXERMSGBasic
    {
        private readonly SQLiteConnection? _connectionOverride;

        protected SQLiteConnection _connection
        {
            get => _connectionOverride ?? AppEnvironment.GetDataBaseManager().GetConnection();
        }

        public TXERMSGBasic()
        {
        }

        public TXERMSGBasic(SQLiteConnection connection)
        {
            _connectionOverride = connection;
        }

        #region Methods PUBLIC
        public virtual T1ERMSG CreateNew()
        {
            T1ERMSG obj = new T1ERMSG();
            obj.State = UIXTableObjectState.New;
            return obj;
        }

        public virtual T1ERMSG Copy(T1ERMSG objFrom)
        {
            T1ERMSG obj = CreateNew();
            obj.ERMSG = objFrom.ERMSG;
            obj.ERHIN = objFrom.ERHIN;
            return obj;
        }

        public virtual T1ERMSG DeepCopy(T1ERMSG objFrom)
        {
            throw new NotImplementedException();
        }

        public virtual void Save(T1ERMSG obj)
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

        public virtual void Delete(string ermnr)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM T1ERMSG WHERE ERMNR = @ERMNR;";
            cmd.Parameters.AddWithValue("@ERMNR", ToDbValue(ermnr));
            cmd.ExecuteNonQuery();
            MigrationFactory.ToCsv.ExportCsvFileFor(_connection, "T1ERMSG", MigrationFactory.ImportType.DevSync);
        }

        public bool Exists(T1ERMSG obj)
        {
            return Exists(obj.ERMNR);
        }

        public bool Exists(string ermnr)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM T1ERMSG WHERE ERMNR = @ERMNR;";
            cmd.Parameters.AddWithValue("@ERMNR", ToDbValue(ermnr));
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        public virtual T1ERMSG? Read(string ermnr)
        {
            UIXQuery query = new UIXQuery(K1ERMSG.Name, _connection);
            query.AddField(K1ERMSG.Name, K1ERMSG.Fields.ERMNR);
            query.AddField(K1ERMSG.Name, K1ERMSG.Fields.ERMSG);
            query.AddField(K1ERMSG.Name, K1ERMSG.Fields.ERHIN);
            query.AddField(K1ERMSG.Name, K1ERMSG.Fields.CRAT);
            query.AddField(K1ERMSG.Name, K1ERMSG.Fields.CRUS);
            query.AddField(K1ERMSG.Name, K1ERMSG.Fields.CHAT);
            query.AddField(K1ERMSG.Name, K1ERMSG.Fields.CHUS);
            query.AddWhere(K1ERMSG.Name, K1ERMSG.Fields.ERMNR, QueryCompareType.EQUALS, ermnr);

            using var reader = query.Execute();
            if (!reader.Read())
            {
                return null;
            }

            T1ERMSG obj = Map(reader);
            obj.AcceptChanges();
            return obj;
        }

        public virtual List<T1ERMSG> ReadAll()
        {
            UIXQuery query = new UIXQuery(K1ERMSG.Name, _connection);
            query.AddField(K1ERMSG.Name, K1ERMSG.Fields.ERMNR);
            query.AddField(K1ERMSG.Name, K1ERMSG.Fields.ERMSG);
            query.AddField(K1ERMSG.Name, K1ERMSG.Fields.ERHIN);
            query.AddField(K1ERMSG.Name, K1ERMSG.Fields.CRAT);
            query.AddField(K1ERMSG.Name, K1ERMSG.Fields.CRUS);
            query.AddField(K1ERMSG.Name, K1ERMSG.Fields.CHAT);
            query.AddField(K1ERMSG.Name, K1ERMSG.Fields.CHUS);
            query.AddOrderBy(K1ERMSG.Name, K1ERMSG.Fields.ERMNR, OrderDirection.ASC);

            List<T1ERMSG> list = new List<T1ERMSG>();
            using var reader = query.Execute();
            while (reader.Read())
            {
                T1ERMSG obj = Map(reader);
                obj.AcceptChanges();
                list.Add(obj);
            }
            return list;
        }
        #endregion

        #region Methods PROTECTED

        protected static T1ERMSG Map(SQLiteDataReader reader)
        {
            T1ERMSG obj = new T1ERMSG();
            obj.ERMNR = UIXQuery.GetString(reader, K1ERMSG.Name, K1ERMSG.Fields.ERMNR, def: "");
            obj.ERMSG = UIXQuery.GetString(reader, K1ERMSG.Name, K1ERMSG.Fields.ERMSG, def: "");
            obj.ERHIN = UIXQuery.GetString(reader, K1ERMSG.Name, K1ERMSG.Fields.ERHIN, def: "");
            obj.CRAT = UIXQuery.GetDateTime(reader, K1ERMSG.Name, K1ERMSG.Fields.CRAT);
            obj.CRUS = UIXQuery.GetString(reader, K1ERMSG.Name, K1ERMSG.Fields.CRUS, def: "");
            obj.CHAT = UIXQuery.GetDateTime(reader, K1ERMSG.Name, K1ERMSG.Fields.CHAT);
            obj.CHUS = UIXQuery.GetString(reader, K1ERMSG.Name, K1ERMSG.Fields.CHUS, def: "");
            obj.State = UIXTableObjectState.Available;
            return obj;
        }
        #endregion

        #region Methods PRIVATE
        private void Insert(T1ERMSG obj)
        {
            obj.CRAT = DateTime.Now;
            obj.CHAT = DateTime.Now;
            obj.CRUS = FnOpSys.GetCurrentUserName();
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO T1ERMSG (ERMNR, ERMSG, ERHIN, CRAT, CRUS, CHAT, CHUS) VALUES (@ERMNR, @ERMSG, @ERHIN, @CRAT, @CRUS, @CHAT, @CHUS);";
            cmd.Parameters.AddWithValue("@ERMNR", ToDbValue(obj.ERMNR));
            cmd.Parameters.AddWithValue("@ERMSG", ToDbValue(obj.ERMSG));
            cmd.Parameters.AddWithValue("@ERHIN", ToDbValue(obj.ERHIN));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CRUS", ToDbValue(obj.CRUS));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@CHUS", ToDbValue(obj.CHUS));
            cmd.ExecuteNonQuery();
        }

        private void Update(T1ERMSG obj)
        {
            obj.CHAT = DateTime.Now;
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE T1ERMSG SET ERMNR = @ERMNR, ERMSG = @ERMSG, ERHIN = @ERHIN, CRAT = @CRAT, CRUS = @CRUS, CHAT = @CHAT, CHUS = @CHUS WHERE ERMNR = @ERMNR;";
            cmd.Parameters.AddWithValue("@ERMNR", ToDbValue(obj.ERMNR));
            cmd.Parameters.AddWithValue("@ERMSG", ToDbValue(obj.ERMSG));
            cmd.Parameters.AddWithValue("@ERHIN", ToDbValue(obj.ERHIN));
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
