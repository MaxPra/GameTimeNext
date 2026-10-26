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
    public class TXMETAPBasic
    {
        private readonly SQLiteConnection? _connectionOverride;

        protected SQLiteConnection _connection
        {
            get => _connectionOverride ?? AppEnvironment.GetDataBaseManager().GetConnection();
        }

        public TXMETAPBasic()
        {
        }

        public TXMETAPBasic(SQLiteConnection connection)
        {
            _connectionOverride = connection;
        }

        #region Methods PUBLIC
        public virtual T1METAP CreateNew()
        {
            T1METAP obj = new T1METAP();
            obj.State = UIXTableObjectState.New;
            return obj;
        }

        public virtual void Save(T1METAP obj)
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

        public virtual void Delete(string menam, string ponam)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM T1METAP WHERE MENAM = @MENAM AND PONAM = @PONAM;";
            cmd.Parameters.AddWithValue("@MENAM", ToDbValue(menam));
            cmd.Parameters.AddWithValue("@PONAM", ToDbValue(ponam));
            cmd.ExecuteNonQuery();
            MigrationFactory.ToCsv.ExportCsvFileFor(_connection, "T1METAP", MigrationFactory.ImportType.DevSync);
        }

        public bool Exists(T1METAP obj)
        {
            return Exists(obj.MENAM, obj.PONAM);
        }

        public bool Exists(string menam, string ponam)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM T1METAP WHERE MENAM = @MENAM AND PONAM = @PONAM;";
            cmd.Parameters.AddWithValue("@MENAM", ToDbValue(menam));
            cmd.Parameters.AddWithValue("@PONAM", ToDbValue(ponam));
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        public virtual T1METAP? Read(string menam, string ponam)
        {
            UIXQuery query = new UIXQuery(K1METAP.Name, _connection);
            query.AddField(K1METAP.Name, K1METAP.Fields.MENAM);
            query.AddField(K1METAP.Name, K1METAP.Fields.PONAM);
            query.AddField(K1METAP.Name, K1METAP.Fields.DESCR);
            query.AddField(K1METAP.Name, K1METAP.Fields.DATYP);
            query.AddField(K1METAP.Name, K1METAP.Fields.DALEN);
            query.AddField(K1METAP.Name, K1METAP.Fields.PORDE);
            query.AddField(K1METAP.Name, K1METAP.Fields.PRIMK);
            query.AddField(K1METAP.Name, K1METAP.Fields.AUTOI);
            query.AddField(K1METAP.Name, K1METAP.Fields.DEFAK);
            query.AddField(K1METAP.Name, K1METAP.Fields.DEFVL);
            query.AddField(K1METAP.Name, K1METAP.Fields.CRAT);
            query.AddField(K1METAP.Name, K1METAP.Fields.CRUS);
            query.AddField(K1METAP.Name, K1METAP.Fields.CHAT);
            query.AddField(K1METAP.Name, K1METAP.Fields.CHUS);
            query.AddWhere(K1METAP.Name, K1METAP.Fields.MENAM, QueryCompareType.EQUALS, menam);
            query.AddWhere(K1METAP.Name, K1METAP.Fields.PONAM, QueryCompareType.EQUALS, ponam);

            using var reader = query.Execute();
            if (!reader.Read())
            {
                return null;
            }

            T1METAP obj = Map(reader);
            obj.AcceptChanges();
            return obj;
        }

        public virtual List<T1METAP> ReadAll()
        {
            UIXQuery query = new UIXQuery(K1METAP.Name, _connection);
            query.AddField(K1METAP.Name, K1METAP.Fields.MENAM);
            query.AddField(K1METAP.Name, K1METAP.Fields.PONAM);
            query.AddField(K1METAP.Name, K1METAP.Fields.DESCR);
            query.AddField(K1METAP.Name, K1METAP.Fields.DATYP);
            query.AddField(K1METAP.Name, K1METAP.Fields.DALEN);
            query.AddField(K1METAP.Name, K1METAP.Fields.PORDE);
            query.AddField(K1METAP.Name, K1METAP.Fields.PRIMK);
            query.AddField(K1METAP.Name, K1METAP.Fields.AUTOI);
            query.AddField(K1METAP.Name, K1METAP.Fields.DEFAK);
            query.AddField(K1METAP.Name, K1METAP.Fields.DEFVL);
            query.AddField(K1METAP.Name, K1METAP.Fields.CRAT);
            query.AddField(K1METAP.Name, K1METAP.Fields.CRUS);
            query.AddField(K1METAP.Name, K1METAP.Fields.CHAT);
            query.AddField(K1METAP.Name, K1METAP.Fields.CHUS);
            query.AddOrderBy(K1METAP.Name, K1METAP.Fields.MENAM, OrderDirection.ASC);
            query.AddOrderBy(K1METAP.Name, K1METAP.Fields.PONAM, OrderDirection.ASC);

            List<T1METAP> list = new List<T1METAP>();
            using var reader = query.Execute();
            while (reader.Read())
            {
                T1METAP obj = Map(reader);
                obj.AcceptChanges();
                list.Add(obj);
            }
            return list;
        }
        #endregion

        #region Methods PROTECTED

        protected static T1METAP Map(SQLiteDataReader reader)
        {
            T1METAP obj = new T1METAP();
            obj.MENAM = UIXQuery.GetString(reader, K1METAP.Name, K1METAP.Fields.MENAM, def: "");
            obj.PONAM = UIXQuery.GetString(reader, K1METAP.Name, K1METAP.Fields.PONAM, def: "");
            obj.DESCR = UIXQuery.GetString(reader, K1METAP.Name, K1METAP.Fields.DESCR, def: "");
            obj.DATYP = UIXQuery.GetString(reader, K1METAP.Name, K1METAP.Fields.DATYP, def: "");
            obj.DALEN = UIXQuery.GetInt32(reader, K1METAP.Name, K1METAP.Fields.DALEN);
            obj.PORDE = UIXQuery.GetInt32(reader, K1METAP.Name, K1METAP.Fields.PORDE);
            obj.PRIMK = UIXQuery.GetBool(reader, K1METAP.Name, K1METAP.Fields.PRIMK);
            obj.AUTOI = UIXQuery.GetBool(reader, K1METAP.Name, K1METAP.Fields.AUTOI);
            obj.DEFAK = UIXQuery.GetBool(reader, K1METAP.Name, K1METAP.Fields.DEFAK);
            obj.DEFVL = UIXQuery.GetString(reader, K1METAP.Name, K1METAP.Fields.DEFVL, def: "");
            obj.CRAT = UIXQuery.GetDateTime(reader, K1METAP.Name, K1METAP.Fields.CRAT);
            obj.CRUS = UIXQuery.GetString(reader, K1METAP.Name, K1METAP.Fields.CRUS, def: "");
            obj.CHAT = UIXQuery.GetDateTime(reader, K1METAP.Name, K1METAP.Fields.CHAT);
            obj.CHUS = UIXQuery.GetString(reader, K1METAP.Name, K1METAP.Fields.CHUS, def: "");
            obj.State = UIXTableObjectState.Available;
            return obj;
        }
        #endregion

        #region Methods PRIVATE
        private void Insert(T1METAP obj)
        {
            obj.CRAT = DateTime.Now;
            obj.CHAT = DateTime.Now;
            obj.CRUS = FnOpSys.GetCurrentUserName();
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO T1METAP (MENAM, PONAM, DESCR, DATYP, DALEN, PORDE, PRIMK, AUTOI, DEFAK, DEFVL, CRAT, CRUS, CHAT, CHUS) VALUES (@MENAM, @PONAM, @DESCR, @DATYP, @DALEN, @PORDE, @PRIMK, @AUTOI, @DEFAK, @DEFVL, @CRAT, @CRUS, @CHAT, @CHUS);";
            cmd.Parameters.AddWithValue("@MENAM", ToDbValue(obj.MENAM));
            cmd.Parameters.AddWithValue("@PONAM", ToDbValue(obj.PONAM));
            cmd.Parameters.AddWithValue("@DESCR", ToDbValue(obj.DESCR));
            cmd.Parameters.AddWithValue("@DATYP", ToDbValue(obj.DATYP));
            cmd.Parameters.AddWithValue("@DALEN", ToDbValue(obj.DALEN));
            cmd.Parameters.AddWithValue("@PORDE", ToDbValue(obj.PORDE));
            cmd.Parameters.AddWithValue("@PRIMK", ToDbValue(obj.PRIMK));
            cmd.Parameters.AddWithValue("@AUTOI", ToDbValue(obj.AUTOI));
            cmd.Parameters.AddWithValue("@DEFAK", ToDbValue(obj.DEFAK));
            cmd.Parameters.AddWithValue("@DEFVL", ToDbValue(obj.DEFVL));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CRUS", ToDbValue(obj.CRUS));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@CHUS", ToDbValue(obj.CHUS));
            cmd.ExecuteNonQuery();
        }

        private void Update(T1METAP obj)
        {
            obj.CHAT = DateTime.Now;
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE T1METAP SET MENAM = @MENAM, PONAM = @PONAM, DESCR = @DESCR, DATYP = @DATYP, DALEN = @DALEN, PORDE = @PORDE, PRIMK = @PRIMK, AUTOI = @AUTOI, DEFAK = @DEFAK, DEFVL = @DEFVL, CRAT = @CRAT, CRUS = @CRUS, CHAT = @CHAT, CHUS = @CHUS WHERE MENAM = @MENAM AND PONAM = @PONAM;";
            cmd.Parameters.AddWithValue("@MENAM", ToDbValue(obj.MENAM));
            cmd.Parameters.AddWithValue("@PONAM", ToDbValue(obj.PONAM));
            cmd.Parameters.AddWithValue("@DESCR", ToDbValue(obj.DESCR));
            cmd.Parameters.AddWithValue("@DATYP", ToDbValue(obj.DATYP));
            cmd.Parameters.AddWithValue("@DALEN", ToDbValue(obj.DALEN));
            cmd.Parameters.AddWithValue("@PORDE", ToDbValue(obj.PORDE));
            cmd.Parameters.AddWithValue("@PRIMK", ToDbValue(obj.PRIMK));
            cmd.Parameters.AddWithValue("@AUTOI", ToDbValue(obj.AUTOI));
            cmd.Parameters.AddWithValue("@DEFAK", ToDbValue(obj.DEFAK));
            cmd.Parameters.AddWithValue("@DEFVL", ToDbValue(obj.DEFVL));
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
