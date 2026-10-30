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
    public class TXPROFIBasic
    {
        private readonly SQLiteConnection? _connectionOverride;

        protected SQLiteConnection _connection
        {
            get => _connectionOverride ?? AppEnvironment.GetDataBaseManager().GetConnection();
        }

        public TXPROFIBasic()
        {
        }

        public TXPROFIBasic(SQLiteConnection connection)
        {
            _connectionOverride = connection;
        }

        #region Methods PUBLIC
        public virtual T1PROFI CreateNew()
        {
            T1PROFI obj = new T1PROFI();
            obj.State = UIXTableObjectState.New;
            return obj;
        }

        public virtual T1PROFI Copy(T1PROFI objFrom)
        {
            T1PROFI obj = CreateNew();
            obj.GANA = objFrom.GANA;
            obj.FIPL = objFrom.FIPL;
            obj.LAPL = objFrom.LAPL;
            obj.PPFN = objFrom.PPFN;
            obj.EXGF = objFrom.EXGF;
            obj.LNTY = objFrom.LNTY;
            obj.LNID = objFrom.LNID;
            obj.SAID = objFrom.SAID;
            obj.PRSE = objFrom.PRSE;
            obj.EXEC = objFrom.EXEC;
            obj.ACCO = objFrom.ACCO;
            obj.ACIN = objFrom.ACIN;
            obj.ACAC = objFrom.ACAC;
            obj.CUPT = objFrom.CUPT;
            obj.ETMA = objFrom.ETMA;
            obj.ETME = objFrom.ETME;
            obj.ETCO = objFrom.ETCO;
            obj.ETTY = objFrom.ETTY;
            obj.ETML = objFrom.ETML;
            obj.ARCH = objFrom.ARCH;
            obj.PLAFO = objFrom.PLAFO;
            return obj;
        }

        public virtual T1PROFI DeepCopy(T1PROFI objFrom)
        {
            throw new NotImplementedException();
        }

        public virtual void Save(T1PROFI obj)
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

        public virtual void Delete(long pfid)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM T1PROFI WHERE PFID = @PFID;";
            cmd.Parameters.AddWithValue("@PFID", ToDbValue(pfid));
            cmd.ExecuteNonQuery();
            MigrationFactory.ToCsv.ExportCsvFileFor(_connection, "T1PROFI", MigrationFactory.ImportType.DevSync);
        }

        public bool Exists(T1PROFI obj)
        {
            return Exists(obj.PFID);
        }

        public bool Exists(long pfid)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM T1PROFI WHERE PFID = @PFID;";
            cmd.Parameters.AddWithValue("@PFID", ToDbValue(pfid));
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        public virtual T1PROFI? Read(long pfid)
        {
            UIXQuery query = new UIXQuery(K1PROFI.Name, _connection);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.PFID);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.GANA);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.FIPL);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.LAPL);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.PPFN);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.EXGF);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.LNTY);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.LNID);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.SAID);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.PRSE);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.EXEC);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ACCO);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ACIN);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ACAC);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.CUPT);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ETMA);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ETME);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ETCO);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ETTY);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ETML);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ARCH);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.PLAFO);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.CRAT);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.CRUS);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.CHAT);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.CHUS);
            query.AddWhere(K1PROFI.Name, K1PROFI.Fields.PFID, QueryCompareType.EQUALS, pfid);

            using var reader = query.Execute();
            if (!reader.Read())
            {
                return null;
            }

            T1PROFI obj = Map(reader);
            obj.AcceptChanges();
            return obj;
        }

        public virtual List<T1PROFI> ReadAll()
        {
            UIXQuery query = new UIXQuery(K1PROFI.Name, _connection);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.PFID);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.GANA);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.FIPL);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.LAPL);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.PPFN);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.EXGF);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.LNTY);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.LNID);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.SAID);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.PRSE);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.EXEC);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ACCO);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ACIN);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ACAC);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.CUPT);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ETMA);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ETME);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ETCO);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ETTY);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ETML);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.ARCH);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.PLAFO);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.CRAT);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.CRUS);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.CHAT);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.CHUS);
            query.AddOrderBy(K1PROFI.Name, K1PROFI.Fields.PFID, OrderDirection.ASC);

            List<T1PROFI> list = new List<T1PROFI>();
            using var reader = query.Execute();
            while (reader.Read())
            {
                T1PROFI obj = Map(reader);
                obj.AcceptChanges();
                list.Add(obj);
            }
            return list;
        }
        #endregion

        #region Methods PROTECTED

        protected static T1PROFI Map(SQLiteDataReader reader)
        {
            T1PROFI obj = new T1PROFI();
            obj.PFID = UIXQuery.GetInt64(reader, K1PROFI.Name, K1PROFI.Fields.PFID);
            obj.GANA = UIXQuery.GetString(reader, K1PROFI.Name, K1PROFI.Fields.GANA, def: "");
            obj.FIPL = UIXQuery.GetDateTime(reader, K1PROFI.Name, K1PROFI.Fields.FIPL);
            obj.LAPL = UIXQuery.GetDateTime(reader, K1PROFI.Name, K1PROFI.Fields.LAPL);
            obj.PPFN = UIXQuery.GetString(reader, K1PROFI.Name, K1PROFI.Fields.PPFN, def: "");
            obj.EXGF = UIXQuery.GetString(reader, K1PROFI.Name, K1PROFI.Fields.EXGF, def: "");
            obj.LNTY = UIXQuery.GetString(reader, K1PROFI.Name, K1PROFI.Fields.LNTY, def: "");
            obj.LNID = UIXQuery.GetString(reader, K1PROFI.Name, K1PROFI.Fields.LNID, def: "");
            obj.SAID = UIXQuery.GetInt64(reader, K1PROFI.Name, K1PROFI.Fields.SAID);
            obj.PRSE = UIXQuery.GetString(reader, K1PROFI.Name, K1PROFI.Fields.PRSE, def: "");
            obj.EXEC = UIXQuery.GetString(reader, K1PROFI.Name, K1PROFI.Fields.EXEC, def: "");
            obj.ACCO = UIXQuery.GetString(reader, K1PROFI.Name, K1PROFI.Fields.ACCO, def: "");
            obj.ACIN = UIXQuery.GetString(reader, K1PROFI.Name, K1PROFI.Fields.ACIN, def: "");
            obj.ACAC = UIXQuery.GetBool(reader, K1PROFI.Name, K1PROFI.Fields.ACAC);
            obj.CUPT = UIXQuery.GetInt64(reader, K1PROFI.Name, K1PROFI.Fields.CUPT);
            obj.ETMA = UIXQuery.GetDouble(reader, K1PROFI.Name, K1PROFI.Fields.ETMA);
            obj.ETME = UIXQuery.GetDouble(reader, K1PROFI.Name, K1PROFI.Fields.ETME);
            obj.ETCO = UIXQuery.GetDouble(reader, K1PROFI.Name, K1PROFI.Fields.ETCO);
            obj.ETTY = UIXQuery.GetString(reader, K1PROFI.Name, K1PROFI.Fields.ETTY, def: "");
            obj.ETML = UIXQuery.GetBool(reader, K1PROFI.Name, K1PROFI.Fields.ETML);
            obj.ARCH = UIXQuery.GetBool(reader, K1PROFI.Name, K1PROFI.Fields.ARCH);
            obj.PLAFO = UIXQuery.GetString(reader, K1PROFI.Name, K1PROFI.Fields.PLAFO, def: "");
            obj.CRAT = UIXQuery.GetDateTime(reader, K1PROFI.Name, K1PROFI.Fields.CRAT);
            obj.CRUS = UIXQuery.GetString(reader, K1PROFI.Name, K1PROFI.Fields.CRUS, def: "");
            obj.CHAT = UIXQuery.GetDateTime(reader, K1PROFI.Name, K1PROFI.Fields.CHAT);
            obj.CHUS = UIXQuery.GetString(reader, K1PROFI.Name, K1PROFI.Fields.CHUS, def: "");
            obj.State = UIXTableObjectState.Available;
            return obj;
        }
        #endregion

        #region Methods PRIVATE
        private void Insert(T1PROFI obj)
        {
            obj.CRAT = DateTime.Now;
            obj.CHAT = DateTime.Now;
            obj.CRUS = FnOpSys.GetCurrentUserName();
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO T1PROFI (GANA, FIPL, LAPL, PPFN, EXGF, LNTY, LNID, SAID, PRSE, EXEC, ACCO, ACIN, ACAC, CUPT, ETMA, ETME, ETCO, ETTY, ETML, ARCH, PLAFO, CRAT, CRUS, CHAT, CHUS) VALUES (@GANA, @FIPL, @LAPL, @PPFN, @EXGF, @LNTY, @LNID, @SAID, @PRSE, @EXEC, @ACCO, @ACIN, @ACAC, @CUPT, @ETMA, @ETME, @ETCO, @ETTY, @ETML, @ARCH, @PLAFO, @CRAT, @CRUS, @CHAT, @CHUS);";
            cmd.Parameters.AddWithValue("@GANA", ToDbValue(obj.GANA));
            cmd.Parameters.AddWithValue("@FIPL", ToDbValue(obj.FIPL));
            cmd.Parameters.AddWithValue("@LAPL", ToDbValue(obj.LAPL));
            cmd.Parameters.AddWithValue("@PPFN", ToDbValue(obj.PPFN));
            cmd.Parameters.AddWithValue("@EXGF", ToDbValue(obj.EXGF));
            cmd.Parameters.AddWithValue("@LNTY", ToDbValue(obj.LNTY));
            cmd.Parameters.AddWithValue("@LNID", ToDbValue(obj.LNID));
            cmd.Parameters.AddWithValue("@SAID", ToDbValue(obj.SAID));
            cmd.Parameters.AddWithValue("@PRSE", ToDbValue(obj.PRSE));
            cmd.Parameters.AddWithValue("@EXEC", ToDbValue(obj.EXEC));
            cmd.Parameters.AddWithValue("@ACCO", ToDbValue(obj.ACCO));
            cmd.Parameters.AddWithValue("@ACIN", ToDbValue(obj.ACIN));
            cmd.Parameters.AddWithValue("@ACAC", ToDbValue(obj.ACAC));
            cmd.Parameters.AddWithValue("@CUPT", ToDbValue(obj.CUPT));
            cmd.Parameters.AddWithValue("@ETMA", ToDbValue(obj.ETMA));
            cmd.Parameters.AddWithValue("@ETME", ToDbValue(obj.ETME));
            cmd.Parameters.AddWithValue("@ETCO", ToDbValue(obj.ETCO));
            cmd.Parameters.AddWithValue("@ETTY", ToDbValue(obj.ETTY));
            cmd.Parameters.AddWithValue("@ETML", ToDbValue(obj.ETML));
            cmd.Parameters.AddWithValue("@ARCH", ToDbValue(obj.ARCH));
            cmd.Parameters.AddWithValue("@PLAFO", ToDbValue(obj.PLAFO));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CRUS", ToDbValue(obj.CRUS));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@CHUS", ToDbValue(obj.CHUS));
            cmd.ExecuteNonQuery();

            using SQLiteCommand idCmd = _connection.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid();";
            obj.PFID = Convert.ToInt64(idCmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private void Update(T1PROFI obj)
        {
            obj.CHAT = DateTime.Now;
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE T1PROFI SET GANA = @GANA, FIPL = @FIPL, LAPL = @LAPL, PPFN = @PPFN, EXGF = @EXGF, LNTY = @LNTY, LNID = @LNID, SAID = @SAID, PRSE = @PRSE, EXEC = @EXEC, ACCO = @ACCO, ACIN = @ACIN, ACAC = @ACAC, CUPT = @CUPT, ETMA = @ETMA, ETME = @ETME, ETCO = @ETCO, ETTY = @ETTY, ETML = @ETML, ARCH = @ARCH, PLAFO = @PLAFO, CRAT = @CRAT, CRUS = @CRUS, CHAT = @CHAT, CHUS = @CHUS WHERE PFID = @PFID;";
            cmd.Parameters.AddWithValue("@PFID", ToDbValue(obj.PFID));
            cmd.Parameters.AddWithValue("@GANA", ToDbValue(obj.GANA));
            cmd.Parameters.AddWithValue("@FIPL", ToDbValue(obj.FIPL));
            cmd.Parameters.AddWithValue("@LAPL", ToDbValue(obj.LAPL));
            cmd.Parameters.AddWithValue("@PPFN", ToDbValue(obj.PPFN));
            cmd.Parameters.AddWithValue("@EXGF", ToDbValue(obj.EXGF));
            cmd.Parameters.AddWithValue("@LNTY", ToDbValue(obj.LNTY));
            cmd.Parameters.AddWithValue("@LNID", ToDbValue(obj.LNID));
            cmd.Parameters.AddWithValue("@SAID", ToDbValue(obj.SAID));
            cmd.Parameters.AddWithValue("@PRSE", ToDbValue(obj.PRSE));
            cmd.Parameters.AddWithValue("@EXEC", ToDbValue(obj.EXEC));
            cmd.Parameters.AddWithValue("@ACCO", ToDbValue(obj.ACCO));
            cmd.Parameters.AddWithValue("@ACIN", ToDbValue(obj.ACIN));
            cmd.Parameters.AddWithValue("@ACAC", ToDbValue(obj.ACAC));
            cmd.Parameters.AddWithValue("@CUPT", ToDbValue(obj.CUPT));
            cmd.Parameters.AddWithValue("@ETMA", ToDbValue(obj.ETMA));
            cmd.Parameters.AddWithValue("@ETME", ToDbValue(obj.ETME));
            cmd.Parameters.AddWithValue("@ETCO", ToDbValue(obj.ETCO));
            cmd.Parameters.AddWithValue("@ETTY", ToDbValue(obj.ETTY));
            cmd.Parameters.AddWithValue("@ETML", ToDbValue(obj.ETML));
            cmd.Parameters.AddWithValue("@ARCH", ToDbValue(obj.ARCH));
            cmd.Parameters.AddWithValue("@PLAFO", ToDbValue(obj.PLAFO));
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
