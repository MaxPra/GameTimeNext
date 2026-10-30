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
    public class TXCTABDBasic
    {
        private readonly SQLiteConnection? _connectionOverride;

        protected SQLiteConnection _connection
        {
            get => _connectionOverride ?? AppEnvironment.GetDataBaseManager().GetConnection();
        }

        public TXCTABDBasic()
        {
        }

        public TXCTABDBasic(SQLiteConnection connection)
        {
            _connectionOverride = connection;
        }

        #region Methods PUBLIC
        public virtual T1CTABD CreateNew()
        {
            T1CTABD obj = new T1CTABD();
            obj.State = UIXTableObjectState.New;
            return obj;
        }

        public virtual T1CTABD Copy(T1CTABD objFrom)
        {
            T1CTABD obj = CreateNew();
            obj.DESCR = objFrom.DESCR;
            obj.PARM1 = objFrom.PARM1;
            obj.PARM2 = objFrom.PARM2;
            return obj;
        }

        public virtual T1CTABD DeepCopy(T1CTABD objFrom)
        {
            throw new NotImplementedException();
        }

        public virtual void Save(T1CTABD obj)
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

        public virtual void Delete(string txtyp, string txnum)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM T1CTABD WHERE TXTYP = @TXTYP AND TXNUM = @TXNUM;";
            cmd.Parameters.AddWithValue("@TXTYP", ToDbValue(txtyp));
            cmd.Parameters.AddWithValue("@TXNUM", ToDbValue(txnum));
            cmd.ExecuteNonQuery();
            MigrationFactory.ToCsv.ExportCsvFileFor(_connection, "T1CTABD", MigrationFactory.ImportType.DevSync);
        }

        public bool Exists(T1CTABD obj)
        {
            return Exists(obj.TXTYP, obj.TXNUM);
        }

        public bool Exists(string txtyp, string txnum)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM T1CTABD WHERE TXTYP = @TXTYP AND TXNUM = @TXNUM;";
            cmd.Parameters.AddWithValue("@TXTYP", ToDbValue(txtyp));
            cmd.Parameters.AddWithValue("@TXNUM", ToDbValue(txnum));
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        public virtual T1CTABD? Read(string txtyp, string txnum)
        {
            UIXQuery query = new UIXQuery(K1CTABD.Name, _connection);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.TXTYP);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.TXNUM);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.DESCR);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.PARM1);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.PARM2);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.CRAT);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.CRUS);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.CHAT);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.CHUS);
            query.AddWhere(K1CTABD.Name, K1CTABD.Fields.TXTYP, QueryCompareType.EQUALS, txtyp);
            query.AddWhere(K1CTABD.Name, K1CTABD.Fields.TXNUM, QueryCompareType.EQUALS, txnum);

            using var reader = query.Execute();
            if (!reader.Read())
            {
                return null;
            }

            T1CTABD obj = Map(reader);
            obj.AcceptChanges();
            return obj;
        }

        public virtual List<T1CTABD> ReadAll()
        {
            UIXQuery query = new UIXQuery(K1CTABD.Name, _connection);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.TXTYP);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.TXNUM);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.DESCR);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.PARM1);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.PARM2);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.CRAT);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.CRUS);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.CHAT);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.CHUS);
            query.AddOrderBy(K1CTABD.Name, K1CTABD.Fields.TXTYP, OrderDirection.ASC);
            query.AddOrderBy(K1CTABD.Name, K1CTABD.Fields.TXNUM, OrderDirection.ASC);

            List<T1CTABD> list = new List<T1CTABD>();
            using var reader = query.Execute();
            while (reader.Read())
            {
                T1CTABD obj = Map(reader);
                obj.AcceptChanges();
                list.Add(obj);
            }
            return list;
        }
        #endregion

        #region Methods PROTECTED

        protected static T1CTABD Map(SQLiteDataReader reader)
        {
            T1CTABD obj = new T1CTABD();
            obj.TXTYP = UIXQuery.GetString(reader, K1CTABD.Name, K1CTABD.Fields.TXTYP, def: "");
            obj.TXNUM = UIXQuery.GetString(reader, K1CTABD.Name, K1CTABD.Fields.TXNUM, def: "");
            obj.DESCR = UIXQuery.GetString(reader, K1CTABD.Name, K1CTABD.Fields.DESCR, def: "");
            obj.PARM1 = UIXQuery.GetString(reader, K1CTABD.Name, K1CTABD.Fields.PARM1, def: "");
            obj.PARM2 = UIXQuery.GetString(reader, K1CTABD.Name, K1CTABD.Fields.PARM2, def: "");
            obj.CRAT = UIXQuery.GetDateTime(reader, K1CTABD.Name, K1CTABD.Fields.CRAT);
            obj.CRUS = UIXQuery.GetString(reader, K1CTABD.Name, K1CTABD.Fields.CRUS, def: "");
            obj.CHAT = UIXQuery.GetDateTime(reader, K1CTABD.Name, K1CTABD.Fields.CHAT);
            obj.CHUS = UIXQuery.GetString(reader, K1CTABD.Name, K1CTABD.Fields.CHUS, def: "");
            obj.State = UIXTableObjectState.Available;
            return obj;
        }
        #endregion

        #region Methods PRIVATE
        private void Insert(T1CTABD obj)
        {
            obj.CRAT = DateTime.Now;
            obj.CHAT = DateTime.Now;
            obj.CRUS = FnOpSys.GetCurrentUserName();
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO T1CTABD (TXTYP, TXNUM, DESCR, PARM1, PARM2, CRAT, CRUS, CHAT, CHUS) VALUES (@TXTYP, @TXNUM, @DESCR, @PARM1, @PARM2, @CRAT, @CRUS, @CHAT, @CHUS);";
            cmd.Parameters.AddWithValue("@TXTYP", ToDbValue(obj.TXTYP));
            cmd.Parameters.AddWithValue("@TXNUM", ToDbValue(obj.TXNUM));
            cmd.Parameters.AddWithValue("@DESCR", ToDbValue(obj.DESCR));
            cmd.Parameters.AddWithValue("@PARM1", ToDbValue(obj.PARM1));
            cmd.Parameters.AddWithValue("@PARM2", ToDbValue(obj.PARM2));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CRUS", ToDbValue(obj.CRUS));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@CHUS", ToDbValue(obj.CHUS));
            cmd.ExecuteNonQuery();
        }

        private void Update(T1CTABD obj)
        {
            obj.CHAT = DateTime.Now;
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE T1CTABD SET TXTYP = @TXTYP, TXNUM = @TXNUM, DESCR = @DESCR, PARM1 = @PARM1, PARM2 = @PARM2, CRAT = @CRAT, CRUS = @CRUS, CHAT = @CHAT, CHUS = @CHUS WHERE TXTYP = @TXTYP AND TXNUM = @TXNUM;";
            cmd.Parameters.AddWithValue("@TXTYP", ToDbValue(obj.TXTYP));
            cmd.Parameters.AddWithValue("@TXNUM", ToDbValue(obj.TXNUM));
            cmd.Parameters.AddWithValue("@DESCR", ToDbValue(obj.DESCR));
            cmd.Parameters.AddWithValue("@PARM1", ToDbValue(obj.PARM1));
            cmd.Parameters.AddWithValue("@PARM2", ToDbValue(obj.PARM2));
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
