using Arcora.Core.Framework;
using Arcora.Core.Framework.DataBase.Migration;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Migration.Types;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Utils;

namespace Arcora.Core.Application.Metadata.Data
{
    public class TXSYMIGBasic : UIXDataManagerBase
    {
        private readonly SQLiteConnection? _connectionOverride;

        protected SQLiteConnection _connection
        {
            get => _connectionOverride ?? AppEnvironment.GetDataBaseManager().GetConnection();
        }

        public TXSYMIGBasic()
        {
        }

        public TXSYMIGBasic(SQLiteConnection connection)
        {
            _connectionOverride = connection;
        }

        #region Methods PUBLIC
        public virtual T1SYMIG CreateNew()
        {
            T1SYMIG obj = new T1SYMIG();
            obj.State = UIXTableObjectState.New;
            return Adopt(obj);
        }

        public virtual T1SYMIG Copy(T1SYMIG objFrom)
        {
            T1SYMIG obj = CreateNew();
            obj.MGAV = objFrom.MGAV;
            return obj;
        }

        public virtual T1SYMIG DeepCopy(T1SYMIG objFrom)
        {
            throw new NotImplementedException();
        }

        public virtual void Save(T1SYMIG obj)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            EnsureNotLocked(obj);

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

        public virtual void Delete(string mgty, string mgid)
        {
            EnsureNotLocked(K1SYMIG.Name, mgty, mgid);

            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM T1SYMIG WHERE MGTY = @MGTY AND MGID = @MGID;";
            cmd.Parameters.AddWithValue("@MGTY", ToDbValue(mgty));
            cmd.Parameters.AddWithValue("@MGID", ToDbValue(mgid));
            cmd.ExecuteNonQuery();
            MigrationFactory.ToCsv.ExportCsvFileFor(_connection, "T1SYMIG", MigrationFactory.ImportType.DevSync);
        }

        public bool Exists(T1SYMIG obj)
        {
            return Exists(obj.MGTY, obj.MGID);
        }

        public bool Exists(string mgty, string mgid)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM T1SYMIG WHERE MGTY = @MGTY AND MGID = @MGID;";
            cmd.Parameters.AddWithValue("@MGTY", ToDbValue(mgty));
            cmd.Parameters.AddWithValue("@MGID", ToDbValue(mgid));
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        public virtual T1SYMIG? Read(string mgty, string mgid)
        {
            UIXQuery query = new UIXQuery(K1SYMIG.Name, _connection);
            query.AddField(K1SYMIG.Name, K1SYMIG.Fields.MGTY);
            query.AddField(K1SYMIG.Name, K1SYMIG.Fields.MGID);
            query.AddField(K1SYMIG.Name, K1SYMIG.Fields.MGAV);
            query.AddField(K1SYMIG.Name, K1SYMIG.Fields.CRAT);
            query.AddField(K1SYMIG.Name, K1SYMIG.Fields.CRUS);
            query.AddField(K1SYMIG.Name, K1SYMIG.Fields.CHAT);
            query.AddField(K1SYMIG.Name, K1SYMIG.Fields.CHUS);
            query.AddWhere(K1SYMIG.Name, K1SYMIG.Fields.MGTY, QueryCompareType.EQUALS, mgty);
            query.AddWhere(K1SYMIG.Name, K1SYMIG.Fields.MGID, QueryCompareType.EQUALS, mgid);

            using var reader = query.Execute();
            if (!reader.Read())
            {
                return null;
            }

            T1SYMIG obj = Adopt(Map(reader));
            obj.AcceptChanges();
            return obj;
        }

        public virtual List<T1SYMIG> ReadAll()
        {
            UIXQuery query = new UIXQuery(K1SYMIG.Name, _connection);
            query.AddField(K1SYMIG.Name, K1SYMIG.Fields.MGTY);
            query.AddField(K1SYMIG.Name, K1SYMIG.Fields.MGID);
            query.AddField(K1SYMIG.Name, K1SYMIG.Fields.MGAV);
            query.AddField(K1SYMIG.Name, K1SYMIG.Fields.CRAT);
            query.AddField(K1SYMIG.Name, K1SYMIG.Fields.CRUS);
            query.AddField(K1SYMIG.Name, K1SYMIG.Fields.CHAT);
            query.AddField(K1SYMIG.Name, K1SYMIG.Fields.CHUS);
            query.AddOrderBy(K1SYMIG.Name, K1SYMIG.Fields.MGTY, OrderDirection.ASC);
            query.AddOrderBy(K1SYMIG.Name, K1SYMIG.Fields.MGID, OrderDirection.ASC);

            List<T1SYMIG> list = new List<T1SYMIG>();
            using var reader = query.Execute();
            while (reader.Read())
            {
                T1SYMIG obj = Adopt(Map(reader));
                obj.AcceptChanges();
                list.Add(obj);
            }
            return list;
        }
        #endregion

        #region Methods PROTECTED
        protected static T1SYMIG Map(SQLiteDataReader reader)
        {
            T1SYMIG obj = new T1SYMIG();
            obj.MGTY = UIXQuery.GetString(reader, K1SYMIG.Name, K1SYMIG.Fields.MGTY, def: "");
            obj.MGID = UIXQuery.GetString(reader, K1SYMIG.Name, K1SYMIG.Fields.MGID, def: "");
            obj.MGAV = UIXQuery.GetString(reader, K1SYMIG.Name, K1SYMIG.Fields.MGAV, def: "");
            obj.CRAT = UIXQuery.GetDateTime(reader, K1SYMIG.Name, K1SYMIG.Fields.CRAT);
            obj.CRUS = UIXQuery.GetString(reader, K1SYMIG.Name, K1SYMIG.Fields.CRUS, def: "");
            obj.CHAT = UIXQuery.GetDateTime(reader, K1SYMIG.Name, K1SYMIG.Fields.CHAT);
            obj.CHUS = UIXQuery.GetString(reader, K1SYMIG.Name, K1SYMIG.Fields.CHUS, def: "");
            obj.State = UIXTableObjectState.Available;
            return obj;
        }
        #endregion

        #region Methods PRIVATE
        private void Insert(T1SYMIG obj)
        {
            obj.CRAT = DateTime.Now;
            obj.CHAT = DateTime.Now;
            obj.CRUS = FnOpSys.GetCurrentUserName();
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO T1SYMIG (MGTY, MGID, MGAV, CRAT, CRUS, CHAT, CHUS) VALUES (@MGTY, @MGID, @MGAV, @CRAT, @CRUS, @CHAT, @CHUS);";
            cmd.Parameters.AddWithValue("@MGTY", ToDbValue(obj.MGTY));
            cmd.Parameters.AddWithValue("@MGID", ToDbValue(obj.MGID));
            cmd.Parameters.AddWithValue("@MGAV", ToDbValue(obj.MGAV));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CRUS", ToDbValue(obj.CRUS));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@CHUS", ToDbValue(obj.CHUS));
            cmd.ExecuteNonQuery();
        }

        private void Update(T1SYMIG obj)
        {
            obj.CHAT = DateTime.Now;
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE T1SYMIG SET MGTY = @MGTY, MGID = @MGID, MGAV = @MGAV, CRAT = @CRAT, CRUS = @CRUS, CHAT = @CHAT, CHUS = @CHUS WHERE MGTY = @MGTY AND MGID = @MGID;";
            cmd.Parameters.AddWithValue("@MGTY", ToDbValue(obj.MGTY));
            cmd.Parameters.AddWithValue("@MGID", ToDbValue(obj.MGID));
            cmd.Parameters.AddWithValue("@MGAV", ToDbValue(obj.MGAV));
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
