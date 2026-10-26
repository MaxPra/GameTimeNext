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
    public class TXGRPPOBasic
    {
        private readonly SQLiteConnection? _connectionOverride;

        protected SQLiteConnection _connection
        {
            get => _connectionOverride ?? AppEnvironment.GetDataBaseManager().GetConnection();
        }

        public TXGRPPOBasic()
        {
        }

        public TXGRPPOBasic(SQLiteConnection connection)
        {
            _connectionOverride = connection;
        }

        #region Methods PUBLIC
        public virtual T1GRPPO CreateNew()
        {
            T1GRPPO obj = new T1GRPPO();
            obj.State = UIXTableObjectState.New;
            return obj;
        }

        public virtual void Save(T1GRPPO obj)
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

        public virtual void Delete(long gpid)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM T1GRPPO WHERE GPID = @GPID;";
            cmd.Parameters.AddWithValue("@GPID", ToDbValue(gpid));
            cmd.ExecuteNonQuery();
            MigrationFactory.ToCsv.ExportCsvFileFor(_connection, "T1GRPPO", MigrationFactory.ImportType.DevSync);
        }

        public bool Exists(T1GRPPO obj)
        {
            return Exists(obj.GPID);
        }

        public bool Exists(long gpid)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM T1GRPPO WHERE GPID = @GPID;";
            cmd.Parameters.AddWithValue("@GPID", ToDbValue(gpid));
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        public virtual T1GRPPO? Read(long gpid)
        {
            UIXQuery query = new UIXQuery(K1GRPPO.Name, _connection);
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.GPID);
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.GRID);
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.PFID);
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.CRAT);
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.CRUS);
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.CHAT);
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.CHUS);
            query.AddWhere(K1GRPPO.Name, K1GRPPO.Fields.GPID, QueryCompareType.EQUALS, gpid);

            using var reader = query.Execute();
            if (!reader.Read())
            {
                return null;
            }

            T1GRPPO obj = Map(reader);
            obj.AcceptChanges();
            return obj;
        }

        public virtual List<T1GRPPO> ReadAll()
        {
            UIXQuery query = new UIXQuery(K1GRPPO.Name, _connection);
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.GPID);
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.GRID);
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.PFID);
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.CRAT);
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.CRUS);
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.CHAT);
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.CHUS);
            query.AddOrderBy(K1GRPPO.Name, K1GRPPO.Fields.GPID, OrderDirection.ASC);

            List<T1GRPPO> list = new List<T1GRPPO>();
            using var reader = query.Execute();
            while (reader.Read())
            {
                T1GRPPO obj = Map(reader);
                obj.AcceptChanges();
                list.Add(obj);
            }
            return list;
        }
        #endregion

        #region Methods PROTECTED

        protected static T1GRPPO Map(SQLiteDataReader reader)
        {
            T1GRPPO obj = new T1GRPPO();
            obj.GPID = UIXQuery.GetInt64(reader, K1GRPPO.Name, K1GRPPO.Fields.GPID);
            obj.GRID = UIXQuery.GetInt64(reader, K1GRPPO.Name, K1GRPPO.Fields.GRID);
            obj.PFID = UIXQuery.GetInt64(reader, K1GRPPO.Name, K1GRPPO.Fields.PFID);
            obj.CRAT = UIXQuery.GetDateTime(reader, K1GRPPO.Name, K1GRPPO.Fields.CRAT);
            obj.CRUS = UIXQuery.GetString(reader, K1GRPPO.Name, K1GRPPO.Fields.CRUS, def: "");
            obj.CHAT = UIXQuery.GetDateTime(reader, K1GRPPO.Name, K1GRPPO.Fields.CHAT);
            obj.CHUS = UIXQuery.GetString(reader, K1GRPPO.Name, K1GRPPO.Fields.CHUS, def: "");
            obj.State = UIXTableObjectState.Available;
            return obj;
        }
        #endregion

        #region Methods PRIVATE
        private void Insert(T1GRPPO obj)
        {
            obj.CRAT = DateTime.Now;
            obj.CHAT = DateTime.Now;
            obj.CRUS = FnOpSys.GetCurrentUserName();
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO T1GRPPO (GRID, PFID, CRAT, CRUS, CHAT, CHUS) VALUES (@GRID, @PFID, @CRAT, @CRUS, @CHAT, @CHUS);";
            cmd.Parameters.AddWithValue("@GRID", ToDbValue(obj.GRID));
            cmd.Parameters.AddWithValue("@PFID", ToDbValue(obj.PFID));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CRUS", ToDbValue(obj.CRUS));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@CHUS", ToDbValue(obj.CHUS));
            cmd.ExecuteNonQuery();

            using SQLiteCommand idCmd = _connection.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid();";
            obj.GPID = Convert.ToInt64(idCmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private void Update(T1GRPPO obj)
        {
            obj.CHAT = DateTime.Now;
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE T1GRPPO SET GRID = @GRID, PFID = @PFID, CRAT = @CRAT, CRUS = @CRUS, CHAT = @CHAT, CHUS = @CHUS WHERE GPID = @GPID;";
            cmd.Parameters.AddWithValue("@GPID", ToDbValue(obj.GPID));
            cmd.Parameters.AddWithValue("@GRID", ToDbValue(obj.GRID));
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
