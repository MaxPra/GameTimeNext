using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase.Migration;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Migration.Types;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Metadata.Data
{
    public class TXMETAHBasic
    {
        private readonly SQLiteConnection? _connectionOverride;

        protected SQLiteConnection _connection
        {
            get => _connectionOverride ?? AppEnvironment.GetDataBaseManager().GetConnection();
        }

        public TXMETAHBasic()
        {
        }

        public TXMETAHBasic(SQLiteConnection connection)
        {
            _connectionOverride = connection;
        }

        #region Methods PUBLIC
        public virtual T1METAH CreateNew()
        {
            T1METAH obj = new T1METAH();
            obj.State = UIXTableObjectState.New;
            return obj;
        }

        public virtual void Save(T1METAH obj)
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

        public virtual void Delete(string menam)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM T1METAH WHERE MENAM = @MENAM;";
            cmd.Parameters.AddWithValue("@MENAM", ToDbValue(menam));
            cmd.ExecuteNonQuery();
            MigrationFactory.ToCsv.ExportCsvFileFor(_connection, "T1METAH", MigrationFactory.ImportType.DevSync);
        }

        public bool Exists(T1METAH obj)
        {
            return Exists(obj.MENAM);
        }

        public bool Exists(string menam)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM T1METAH WHERE MENAM = @MENAM;";
            cmd.Parameters.AddWithValue("@MENAM", ToDbValue(menam));
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        public virtual T1METAH? Read(string menam)
        {
            UIXQuery query = new UIXQuery(K1METAH.Name, _connection);
            query.AddField(K1METAH.Name, K1METAH.Fields.MENAM);
            query.AddField(K1METAH.Name, K1METAH.Fields.DESCR);
            query.AddField(K1METAH.Name, K1METAH.Fields.MTYPE);
            query.AddField(K1METAH.Name, K1METAH.Fields.DSYNC);
            query.AddField(K1METAH.Name, K1METAH.Fields.GENER);
            query.AddField(K1METAH.Name, K1METAH.Fields.CRAT);
            query.AddField(K1METAH.Name, K1METAH.Fields.CRUS);
            query.AddField(K1METAH.Name, K1METAH.Fields.CHAT);
            query.AddField(K1METAH.Name, K1METAH.Fields.CHUS);
            query.AddWhere(K1METAH.Name, K1METAH.Fields.MENAM, QueryCompareType.EQUALS, menam);

            using var reader = query.Execute();
            if (!reader.Read())
            {
                return null;
            }

            T1METAH obj = Map(reader);
            obj.AcceptChanges();
            return obj;
        }

        public virtual List<T1METAH> ReadAll()
        {
            UIXQuery query = new UIXQuery(K1METAH.Name, _connection);
            query.AddField(K1METAH.Name, K1METAH.Fields.MENAM);
            query.AddField(K1METAH.Name, K1METAH.Fields.DESCR);
            query.AddField(K1METAH.Name, K1METAH.Fields.MTYPE);
            query.AddField(K1METAH.Name, K1METAH.Fields.DSYNC);
            query.AddField(K1METAH.Name, K1METAH.Fields.GENER);
            query.AddField(K1METAH.Name, K1METAH.Fields.CRAT);
            query.AddField(K1METAH.Name, K1METAH.Fields.CRUS);
            query.AddField(K1METAH.Name, K1METAH.Fields.CHAT);
            query.AddField(K1METAH.Name, K1METAH.Fields.CHUS);
            query.AddOrderBy(K1METAH.Name, K1METAH.Fields.MENAM, OrderDirection.ASC);

            List<T1METAH> list = new List<T1METAH>();
            using var reader = query.Execute();
            while (reader.Read())
            {
                T1METAH obj = Map(reader);
                obj.AcceptChanges();
                list.Add(obj);
            }
            return list;
        }
        #endregion

        #region Methods PROTECTED

        protected static T1METAH Map(SQLiteDataReader reader)
        {
            T1METAH obj = new T1METAH();
            obj.MENAM = UIXQuery.GetString(reader, K1METAH.Name, K1METAH.Fields.MENAM, def: "");
            obj.DESCR = UIXQuery.GetString(reader, K1METAH.Name, K1METAH.Fields.DESCR, def: "");
            obj.MTYPE = UIXQuery.GetString(reader, K1METAH.Name, K1METAH.Fields.MTYPE, def: "");
            obj.DSYNC = UIXQuery.GetBool(reader, K1METAH.Name, K1METAH.Fields.DSYNC);
            obj.GENER = UIXQuery.GetBool(reader, K1METAH.Name, K1METAH.Fields.GENER);
            obj.CRAT = UIXQuery.GetDateTime(reader, K1METAH.Name, K1METAH.Fields.CRAT);
            obj.CRUS = UIXQuery.GetString(reader, K1METAH.Name, K1METAH.Fields.CRUS, def: "");
            obj.CHAT = UIXQuery.GetDateTime(reader, K1METAH.Name, K1METAH.Fields.CHAT);
            obj.CHUS = UIXQuery.GetString(reader, K1METAH.Name, K1METAH.Fields.CHUS, def: "");
            obj.State = UIXTableObjectState.Available;
            return obj;
        }
        #endregion

        #region Methods PRIVATE
        private void Insert(T1METAH obj)
        {
            obj.CRAT = DateTime.Now;
            obj.CHAT = DateTime.Now;
            obj.CRUS = FnOpSys.GetCurrentUserName();
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO T1METAH (MENAM, DESCR, MTYPE, DSYNC, GENER, CRAT, CRUS, CHAT, CHUS) VALUES (@MENAM, @DESCR, @MTYPE, @DSYNC, @GENER, @CRAT, @CRUS, @CHAT, @CHUS);";
            cmd.Parameters.AddWithValue("@MENAM", ToDbValue(obj.MENAM));
            cmd.Parameters.AddWithValue("@DESCR", ToDbValue(obj.DESCR));
            cmd.Parameters.AddWithValue("@MTYPE", ToDbValue(obj.MTYPE));
            cmd.Parameters.AddWithValue("@DSYNC", ToDbValue(obj.DSYNC));
            cmd.Parameters.AddWithValue("@GENER", ToDbValue(obj.GENER));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CRUS", ToDbValue(obj.CRUS));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@CHUS", ToDbValue(obj.CHUS));
            cmd.ExecuteNonQuery();
        }

        private void Update(T1METAH obj)
        {
            obj.CHAT = DateTime.Now;
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE T1METAH SET MENAM = @MENAM, DESCR = @DESCR, MTYPE = @MTYPE, DSYNC = @DSYNC, GENER = @GENER, CRAT = @CRAT, CRUS = @CRUS, CHAT = @CHAT, CHUS = @CHUS WHERE MENAM = @MENAM;";
            cmd.Parameters.AddWithValue("@MENAM", ToDbValue(obj.MENAM));
            cmd.Parameters.AddWithValue("@DESCR", ToDbValue(obj.DESCR));
            cmd.Parameters.AddWithValue("@MTYPE", ToDbValue(obj.MTYPE));
            cmd.Parameters.AddWithValue("@DSYNC", ToDbValue(obj.DSYNC));
            cmd.Parameters.AddWithValue("@GENER", ToDbValue(obj.GENER));
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
