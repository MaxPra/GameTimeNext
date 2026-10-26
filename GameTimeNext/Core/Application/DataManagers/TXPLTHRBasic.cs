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
    public class TXPLTHRBasic
    {
        private readonly SQLiteConnection? _connectionOverride;

        protected SQLiteConnection _connection
        {
            get => _connectionOverride ?? AppEnvironment.GetDataBaseManager().GetConnection();
        }

        public TXPLTHRBasic()
        {
        }

        public TXPLTHRBasic(SQLiteConnection connection)
        {
            _connectionOverride = connection;
        }

        #region Methods PUBLIC
        public virtual T1PLTHR CreateNew()
        {
            T1PLTHR obj = new T1PLTHR();
            obj.State = UIXTableObjectState.New;
            return obj;
        }

        public virtual void Save(T1PLTHR obj)
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

        public virtual void Delete(long ptid)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM T1PLTHR WHERE PTID = @PTID;";
            cmd.Parameters.AddWithValue("@PTID", ToDbValue(ptid));
            cmd.ExecuteNonQuery();
            MigrationFactory.ToCsv.ExportCsvFileFor(_connection, "T1PLTHR", MigrationFactory.ImportType.DevSync);
        }

        public bool Exists(T1PLTHR obj)
        {
            return Exists(obj.PTID);
        }

        public bool Exists(long ptid)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM T1PLTHR WHERE PTID = @PTID;";
            cmd.Parameters.AddWithValue("@PTID", ToDbValue(ptid));
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        public virtual T1PLTHR? Read(long ptid)
        {
            UIXQuery query = new UIXQuery(K1PLTHR.Name, _connection);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTID);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PFID);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTTY);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTDE);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTCO);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTCA);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTPA);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTRA);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTRC);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.CRAT);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.CRUS);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.CHAT);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.CHUS);
            query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PTID, QueryCompareType.EQUALS, ptid);

            using var reader = query.Execute();
            if (!reader.Read())
            {
                return null;
            }

            T1PLTHR obj = Map(reader);
            obj.AcceptChanges();
            return obj;
        }

        public virtual List<T1PLTHR> ReadAll()
        {
            UIXQuery query = new UIXQuery(K1PLTHR.Name, _connection);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTID);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PFID);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTTY);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTDE);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTCO);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTCA);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTPA);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTRA);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTRC);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.CRAT);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.CRUS);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.CHAT);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.CHUS);
            query.AddOrderBy(K1PLTHR.Name, K1PLTHR.Fields.PTID, OrderDirection.ASC);

            List<T1PLTHR> list = new List<T1PLTHR>();
            using var reader = query.Execute();
            while (reader.Read())
            {
                T1PLTHR obj = Map(reader);
                obj.AcceptChanges();
                list.Add(obj);
            }
            return list;
        }
        #endregion

        #region Methods PROTECTED

        protected static T1PLTHR Map(SQLiteDataReader reader)
        {
            T1PLTHR obj = new T1PLTHR();
            obj.PTID = UIXQuery.GetInt64(reader, K1PLTHR.Name, K1PLTHR.Fields.PTID);
            obj.PFID = UIXQuery.GetInt64(reader, K1PLTHR.Name, K1PLTHR.Fields.PFID);
            obj.PTTY = UIXQuery.GetString(reader, K1PLTHR.Name, K1PLTHR.Fields.PTTY, def: "");
            obj.PTDE = UIXQuery.GetString(reader, K1PLTHR.Name, K1PLTHR.Fields.PTDE, def: "");
            obj.PTCO = UIXQuery.GetBool(reader, K1PLTHR.Name, K1PLTHR.Fields.PTCO);
            obj.PTCA = UIXQuery.GetBool(reader, K1PLTHR.Name, K1PLTHR.Fields.PTCA);
            obj.PTPA = UIXQuery.GetBool(reader, K1PLTHR.Name, K1PLTHR.Fields.PTPA);
            obj.PTRA = UIXQuery.GetInt32(reader, K1PLTHR.Name, K1PLTHR.Fields.PTRA);
            obj.PTRC = UIXQuery.GetString(reader, K1PLTHR.Name, K1PLTHR.Fields.PTRC, def: "");
            obj.CRAT = UIXQuery.GetDateTime(reader, K1PLTHR.Name, K1PLTHR.Fields.CRAT);
            obj.CRUS = UIXQuery.GetString(reader, K1PLTHR.Name, K1PLTHR.Fields.CRUS, def: "");
            obj.CHAT = UIXQuery.GetDateTime(reader, K1PLTHR.Name, K1PLTHR.Fields.CHAT);
            obj.CHUS = UIXQuery.GetString(reader, K1PLTHR.Name, K1PLTHR.Fields.CHUS, def: "");
            obj.State = UIXTableObjectState.Available;
            return obj;
        }
        #endregion

        #region Methods PRIVATE
        private void Insert(T1PLTHR obj)
        {
            obj.CRAT = DateTime.Now;
            obj.CHAT = DateTime.Now;
            obj.CRUS = FnOpSys.GetCurrentUserName();
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO T1PLTHR (PFID, PTTY, PTDE, PTCO, PTCA, PTPA, PTRA, PTRC, CRAT, CRUS, CHAT, CHUS) VALUES (@PFID, @PTTY, @PTDE, @PTCO, @PTCA, @PTPA, @PTRA, @PTRC, @CRAT, @CRUS, @CHAT, @CHUS);";
            cmd.Parameters.AddWithValue("@PFID", ToDbValue(obj.PFID));
            cmd.Parameters.AddWithValue("@PTTY", ToDbValue(obj.PTTY));
            cmd.Parameters.AddWithValue("@PTDE", ToDbValue(obj.PTDE));
            cmd.Parameters.AddWithValue("@PTCO", ToDbValue(obj.PTCO));
            cmd.Parameters.AddWithValue("@PTCA", ToDbValue(obj.PTCA));
            cmd.Parameters.AddWithValue("@PTPA", ToDbValue(obj.PTPA));
            cmd.Parameters.AddWithValue("@PTRA", ToDbValue(obj.PTRA));
            cmd.Parameters.AddWithValue("@PTRC", ToDbValue(obj.PTRC));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CRUS", ToDbValue(obj.CRUS));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@CHUS", ToDbValue(obj.CHUS));
            cmd.ExecuteNonQuery();

            using SQLiteCommand idCmd = _connection.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid();";
            obj.PTID = Convert.ToInt64(idCmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private void Update(T1PLTHR obj)
        {
            obj.CHAT = DateTime.Now;
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE T1PLTHR SET PFID = @PFID, PTTY = @PTTY, PTDE = @PTDE, PTCO = @PTCO, PTCA = @PTCA, PTPA = @PTPA, PTRA = @PTRA, PTRC = @PTRC, CRAT = @CRAT, CRUS = @CRUS, CHAT = @CHAT, CHUS = @CHUS WHERE PTID = @PTID;";
            cmd.Parameters.AddWithValue("@PTID", ToDbValue(obj.PTID));
            cmd.Parameters.AddWithValue("@PFID", ToDbValue(obj.PFID));
            cmd.Parameters.AddWithValue("@PTTY", ToDbValue(obj.PTTY));
            cmd.Parameters.AddWithValue("@PTDE", ToDbValue(obj.PTDE));
            cmd.Parameters.AddWithValue("@PTCO", ToDbValue(obj.PTCO));
            cmd.Parameters.AddWithValue("@PTCA", ToDbValue(obj.PTCA));
            cmd.Parameters.AddWithValue("@PTPA", ToDbValue(obj.PTPA));
            cmd.Parameters.AddWithValue("@PTRA", ToDbValue(obj.PTRA));
            cmd.Parameters.AddWithValue("@PTRC", ToDbValue(obj.PTRC));
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
