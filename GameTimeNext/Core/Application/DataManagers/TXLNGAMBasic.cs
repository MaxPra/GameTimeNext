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
    public class TXLNGAMBasic
    {
        private readonly SQLiteConnection? _connectionOverride;

        protected SQLiteConnection _connection
        {
            get => _connectionOverride ?? AppEnvironment.GetDataBaseManager().GetConnection();
        }

        public TXLNGAMBasic()
        {
        }

        public TXLNGAMBasic(SQLiteConnection connection)
        {
            _connectionOverride = connection;
        }

        #region Methods PUBLIC
        public virtual T1LNGAM CreateNew()
        {
            T1LNGAM obj = new T1LNGAM();
            obj.State = UIXTableObjectState.New;
            return obj;
        }

        public virtual T1LNGAM Copy(T1LNGAM objFrom)
        {
            T1LNGAM obj = CreateNew();
            obj.LNTY = objFrom.LNTY;
            obj.LNID = objFrom.LNID;
            obj.GANA = objFrom.GANA;
            obj.LGST = objFrom.LGST;
            obj.PFID = objFrom.PFID;
            return obj;
        }

        public virtual T1LNGAM DeepCopy(T1LNGAM objFrom)
        {
            throw new NotImplementedException();
        }

        public virtual void Save(T1LNGAM obj)
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

        public virtual void Delete(long lgid)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM T1LNGAM WHERE LGID = @LGID;";
            cmd.Parameters.AddWithValue("@LGID", ToDbValue(lgid));
            cmd.ExecuteNonQuery();
            MigrationFactory.ToCsv.ExportCsvFileFor(_connection, "T1LNGAM", MigrationFactory.ImportType.DevSync);
        }

        public bool Exists(T1LNGAM obj)
        {
            return Exists(obj.LGID);
        }

        public bool Exists(long lgid)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM T1LNGAM WHERE LGID = @LGID;";
            cmd.Parameters.AddWithValue("@LGID", ToDbValue(lgid));
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        public virtual T1LNGAM? Read(long lgid)
        {
            UIXQuery query = new UIXQuery(K1LNGAM.Name, _connection);
            query.AddField(K1LNGAM.Name, K1LNGAM.Fields.LGID);
            query.AddField(K1LNGAM.Name, K1LNGAM.Fields.LNTY);
            query.AddField(K1LNGAM.Name, K1LNGAM.Fields.LNID);
            query.AddField(K1LNGAM.Name, K1LNGAM.Fields.GANA);
            query.AddField(K1LNGAM.Name, K1LNGAM.Fields.LGST);
            query.AddField(K1LNGAM.Name, K1LNGAM.Fields.PFID);
            query.AddField(K1LNGAM.Name, K1LNGAM.Fields.CRAT);
            query.AddField(K1LNGAM.Name, K1LNGAM.Fields.CRUS);
            query.AddField(K1LNGAM.Name, K1LNGAM.Fields.CHAT);
            query.AddField(K1LNGAM.Name, K1LNGAM.Fields.CHUS);
            query.AddWhere(K1LNGAM.Name, K1LNGAM.Fields.LGID, QueryCompareType.EQUALS, lgid);

            using var reader = query.Execute();
            if (!reader.Read())
            {
                return null;
            }

            T1LNGAM obj = Map(reader);
            obj.AcceptChanges();
            return obj;
        }

        public virtual List<T1LNGAM> ReadAll()
        {
            UIXQuery query = new UIXQuery(K1LNGAM.Name, _connection);
            query.AddField(K1LNGAM.Name, K1LNGAM.Fields.LGID);
            query.AddField(K1LNGAM.Name, K1LNGAM.Fields.LNTY);
            query.AddField(K1LNGAM.Name, K1LNGAM.Fields.LNID);
            query.AddField(K1LNGAM.Name, K1LNGAM.Fields.GANA);
            query.AddField(K1LNGAM.Name, K1LNGAM.Fields.LGST);
            query.AddField(K1LNGAM.Name, K1LNGAM.Fields.PFID);
            query.AddField(K1LNGAM.Name, K1LNGAM.Fields.CRAT);
            query.AddField(K1LNGAM.Name, K1LNGAM.Fields.CRUS);
            query.AddField(K1LNGAM.Name, K1LNGAM.Fields.CHAT);
            query.AddField(K1LNGAM.Name, K1LNGAM.Fields.CHUS);
            query.AddOrderBy(K1LNGAM.Name, K1LNGAM.Fields.LGID, OrderDirection.ASC);

            List<T1LNGAM> list = new List<T1LNGAM>();
            using var reader = query.Execute();
            while (reader.Read())
            {
                T1LNGAM obj = Map(reader);
                obj.AcceptChanges();
                list.Add(obj);
            }
            return list;
        }
        #endregion

        #region Methods PROTECTED

        protected static T1LNGAM Map(SQLiteDataReader reader)
        {
            T1LNGAM obj = new T1LNGAM();
            obj.LGID = UIXQuery.GetInt64(reader, K1LNGAM.Name, K1LNGAM.Fields.LGID);
            obj.LNTY = UIXQuery.GetString(reader, K1LNGAM.Name, K1LNGAM.Fields.LNTY, def: "");
            obj.LNID = UIXQuery.GetString(reader, K1LNGAM.Name, K1LNGAM.Fields.LNID, def: "");
            obj.GANA = UIXQuery.GetString(reader, K1LNGAM.Name, K1LNGAM.Fields.GANA, def: "");
            obj.LGST = UIXQuery.GetString(reader, K1LNGAM.Name, K1LNGAM.Fields.LGST, def: "");
            obj.PFID = UIXQuery.GetInt64(reader, K1LNGAM.Name, K1LNGAM.Fields.PFID);
            obj.CRAT = UIXQuery.GetDateTime(reader, K1LNGAM.Name, K1LNGAM.Fields.CRAT);
            obj.CRUS = UIXQuery.GetString(reader, K1LNGAM.Name, K1LNGAM.Fields.CRUS, def: "");
            obj.CHAT = UIXQuery.GetDateTime(reader, K1LNGAM.Name, K1LNGAM.Fields.CHAT);
            obj.CHUS = UIXQuery.GetString(reader, K1LNGAM.Name, K1LNGAM.Fields.CHUS, def: "");
            obj.State = UIXTableObjectState.Available;
            return obj;
        }
        #endregion

        #region Methods PRIVATE
        private void Insert(T1LNGAM obj)
        {
            obj.CRAT = DateTime.Now;
            obj.CHAT = DateTime.Now;
            obj.CRUS = FnOpSys.GetCurrentUserName();
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO T1LNGAM (LNTY, LNID, GANA, LGST, PFID, CRAT, CRUS, CHAT, CHUS) VALUES (@LNTY, @LNID, @GANA, @LGST, @PFID, @CRAT, @CRUS, @CHAT, @CHUS);";
            cmd.Parameters.AddWithValue("@LNTY", ToDbValue(obj.LNTY));
            cmd.Parameters.AddWithValue("@LNID", ToDbValue(obj.LNID));
            cmd.Parameters.AddWithValue("@GANA", ToDbValue(obj.GANA));
            cmd.Parameters.AddWithValue("@LGST", ToDbValue(obj.LGST));
            cmd.Parameters.AddWithValue("@PFID", ToDbValue(obj.PFID));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CRUS", ToDbValue(obj.CRUS));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@CHUS", ToDbValue(obj.CHUS));
            cmd.ExecuteNonQuery();

            using SQLiteCommand idCmd = _connection.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid();";
            obj.LGID = Convert.ToInt64(idCmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private void Update(T1LNGAM obj)
        {
            obj.CHAT = DateTime.Now;
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE T1LNGAM SET LNTY = @LNTY, LNID = @LNID, GANA = @GANA, LGST = @LGST, PFID = @PFID, CRAT = @CRAT, CRUS = @CRUS, CHAT = @CHAT, CHUS = @CHUS WHERE LGID = @LGID;";
            cmd.Parameters.AddWithValue("@LGID", ToDbValue(obj.LGID));
            cmd.Parameters.AddWithValue("@LNTY", ToDbValue(obj.LNTY));
            cmd.Parameters.AddWithValue("@LNID", ToDbValue(obj.LNID));
            cmd.Parameters.AddWithValue("@GANA", ToDbValue(obj.GANA));
            cmd.Parameters.AddWithValue("@LGST", ToDbValue(obj.LGST));
            cmd.Parameters.AddWithValue("@PFID", ToDbValue(obj.PFID));
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
