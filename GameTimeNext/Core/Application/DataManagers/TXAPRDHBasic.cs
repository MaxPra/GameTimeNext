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
    public class TXAPRDHBasic
    {
        private readonly SQLiteConnection? _connectionOverride;

        protected SQLiteConnection _connection
        {
            get => _connectionOverride ?? AppEnvironment.GetDataBaseManager().GetConnection();
        }

        public TXAPRDHBasic()
        {
        }

        public TXAPRDHBasic(SQLiteConnection connection)
        {
            _connectionOverride = connection;
        }

        #region Methods PUBLIC
        public virtual T1APRDH CreateNew()
        {
            T1APRDH obj = new T1APRDH();
            obj.State = UIXTableObjectState.New;
            return obj;
        }

        public virtual T1APRDH Copy(T1APRDH objFrom)
        {
            T1APRDH obj = CreateNew();
            obj.APDES = objFrom.APDES;
            obj.APHPT = objFrom.APHPT;
            obj.APMCP = objFrom.APMCP;
            return obj;
        }

        public virtual T1APRDH DeepCopy(T1APRDH objFrom)
        {
            throw new NotImplementedException();
        }

        public virtual void Save(T1APRDH obj)
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

        public virtual void Delete(string apdid)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM T1APRDH WHERE APDID = @APDID;";
            cmd.Parameters.AddWithValue("@APDID", ToDbValue(apdid));
            cmd.ExecuteNonQuery();
            MigrationFactory.ToCsv.ExportCsvFileFor(_connection, "T1APRDH", MigrationFactory.ImportType.DevSync);
        }

        public bool Exists(T1APRDH obj)
        {
            return Exists(obj.APDID);
        }

        public bool Exists(string apdid)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM T1APRDH WHERE APDID = @APDID;";
            cmd.Parameters.AddWithValue("@APDID", ToDbValue(apdid));
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        public virtual T1APRDH? Read(string apdid)
        {
            UIXQuery query = new UIXQuery(K1APRDH.Name, _connection);
            query.AddField(K1APRDH.Name, K1APRDH.Fields.APDID);
            query.AddField(K1APRDH.Name, K1APRDH.Fields.APDES);
            query.AddField(K1APRDH.Name, K1APRDH.Fields.APHPT);
            query.AddField(K1APRDH.Name, K1APRDH.Fields.APMCP);
            query.AddField(K1APRDH.Name, K1APRDH.Fields.CRAT);
            query.AddField(K1APRDH.Name, K1APRDH.Fields.CRUS);
            query.AddField(K1APRDH.Name, K1APRDH.Fields.CHAT);
            query.AddField(K1APRDH.Name, K1APRDH.Fields.CHUS);
            query.AddWhere(K1APRDH.Name, K1APRDH.Fields.APDID, QueryCompareType.EQUALS, apdid);

            using var reader = query.Execute();
            if (!reader.Read())
            {
                return null;
            }

            T1APRDH obj = Map(reader);
            obj.AcceptChanges();
            return obj;
        }

        public virtual List<T1APRDH> ReadAll()
        {
            UIXQuery query = new UIXQuery(K1APRDH.Name, _connection);
            query.AddField(K1APRDH.Name, K1APRDH.Fields.APDID);
            query.AddField(K1APRDH.Name, K1APRDH.Fields.APDES);
            query.AddField(K1APRDH.Name, K1APRDH.Fields.APHPT);
            query.AddField(K1APRDH.Name, K1APRDH.Fields.APMCP);
            query.AddField(K1APRDH.Name, K1APRDH.Fields.CRAT);
            query.AddField(K1APRDH.Name, K1APRDH.Fields.CRUS);
            query.AddField(K1APRDH.Name, K1APRDH.Fields.CHAT);
            query.AddField(K1APRDH.Name, K1APRDH.Fields.CHUS);
            query.AddOrderBy(K1APRDH.Name, K1APRDH.Fields.APDID, OrderDirection.ASC);

            List<T1APRDH> list = new List<T1APRDH>();
            using var reader = query.Execute();
            while (reader.Read())
            {
                T1APRDH obj = Map(reader);
                obj.AcceptChanges();
                list.Add(obj);
            }
            return list;
        }
        #endregion

        #region Methods PROTECTED

        protected static T1APRDH Map(SQLiteDataReader reader)
        {
            T1APRDH obj = new T1APRDH();
            obj.APDID = UIXQuery.GetString(reader, K1APRDH.Name, K1APRDH.Fields.APDID, def: "");
            obj.APDES = UIXQuery.GetString(reader, K1APRDH.Name, K1APRDH.Fields.APDES, def: "");
            obj.APHPT = UIXQuery.GetString(reader, K1APRDH.Name, K1APRDH.Fields.APHPT, def: "");
            obj.APMCP = UIXQuery.GetBool(reader, K1APRDH.Name, K1APRDH.Fields.APMCP);
            obj.CRAT = UIXQuery.GetDateTime(reader, K1APRDH.Name, K1APRDH.Fields.CRAT);
            obj.CRUS = UIXQuery.GetString(reader, K1APRDH.Name, K1APRDH.Fields.CRUS, def: "");
            obj.CHAT = UIXQuery.GetDateTime(reader, K1APRDH.Name, K1APRDH.Fields.CHAT);
            obj.CHUS = UIXQuery.GetString(reader, K1APRDH.Name, K1APRDH.Fields.CHUS, def: "");
            obj.State = UIXTableObjectState.Available;
            return obj;
        }
        #endregion

        #region Methods PRIVATE
        private void Insert(T1APRDH obj)
        {
            obj.CRAT = DateTime.Now;
            obj.CHAT = DateTime.Now;
            obj.CRUS = FnOpSys.GetCurrentUserName();
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO T1APRDH (APDID, APDES, APHPT, APMCP, CRAT, CRUS, CHAT, CHUS) VALUES (@APDID, @APDES, @APHPT, @APMCP, @CRAT, @CRUS, @CHAT, @CHUS);";
            cmd.Parameters.AddWithValue("@APDID", ToDbValue(obj.APDID));
            cmd.Parameters.AddWithValue("@APDES", ToDbValue(obj.APDES));
            cmd.Parameters.AddWithValue("@APHPT", ToDbValue(obj.APHPT));
            cmd.Parameters.AddWithValue("@APMCP", ToDbValue(obj.APMCP));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CRUS", ToDbValue(obj.CRUS));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@CHUS", ToDbValue(obj.CHUS));
            cmd.ExecuteNonQuery();
        }

        private void Update(T1APRDH obj)
        {
            obj.CHAT = DateTime.Now;
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE T1APRDH SET APDID = @APDID, APDES = @APDES, APHPT = @APHPT, APMCP = @APMCP, CRAT = @CRAT, CRUS = @CRUS, CHAT = @CHAT, CHUS = @CHUS WHERE APDID = @APDID;";
            cmd.Parameters.AddWithValue("@APDID", ToDbValue(obj.APDID));
            cmd.Parameters.AddWithValue("@APDES", ToDbValue(obj.APDES));
            cmd.Parameters.AddWithValue("@APHPT", ToDbValue(obj.APHPT));
            cmd.Parameters.AddWithValue("@APMCP", ToDbValue(obj.APMCP));
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
