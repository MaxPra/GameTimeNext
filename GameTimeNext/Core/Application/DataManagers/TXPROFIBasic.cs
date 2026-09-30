using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase.Migration;
using System.Data.SQLite;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXPROFIBasic
    {
        protected SQLiteConnection _connection
        {
            get => AppEnvironment.GetDataBaseManager().GetConnection();
        }

        #region Methods PUBLIC
        public virtual T1PROFI CreateNew()
        {
            T1PROFI obj = new T1PROFI();
            obj.State = UIXTableObjectState.New;
            return obj;
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
            string sql = $"DELETE FROM T1PROFI WHERE PFID = '{pfid}';";
            UIXQuery.ExecuteCustom(sql, _connection);
            MigrationFactory.ToCsv.ExportCsvFileFor(_connection, "T1PROFI", MigrationFactory.ImportType.DevSync);
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
            query.AddField(K1PROFI.Name, K1PROFI.Fields.SAID);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.PRSE);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.EXEC);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.CRAT);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.CHAT);
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
            query.AddField(K1PROFI.Name, K1PROFI.Fields.SAID);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.PRSE);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.EXEC);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.CRAT);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.CHAT);
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

        #region Methods PRIVATE

        protected static T1PROFI Map(SQLiteDataReader reader)
        {
            T1PROFI obj = new T1PROFI();
            obj.PFID = UIXQuery.GetInt64(reader, K1PROFI.Name, K1PROFI.Fields.PFID);
            obj.GANA = UIXQuery.GetString(reader, K1PROFI.Name, K1PROFI.Fields.GANA, def: "");
            obj.FIPL = UIXQuery.GetDateTime(reader, K1PROFI.Name, K1PROFI.Fields.FIPL);
            obj.LAPL = UIXQuery.GetDateTime(reader, K1PROFI.Name, K1PROFI.Fields.LAPL);
            obj.PPFN = UIXQuery.GetString(reader, K1PROFI.Name, K1PROFI.Fields.PPFN, def: "");
            obj.EXGF = UIXQuery.GetString(reader, K1PROFI.Name, K1PROFI.Fields.EXGF, def: "");
            obj.SAID = UIXQuery.GetInt64(reader, K1PROFI.Name, K1PROFI.Fields.SAID);
            obj.PRSE = UIXQuery.GetString(reader, K1PROFI.Name, K1PROFI.Fields.PRSE, def: "");
            obj.EXEC = UIXQuery.GetString(reader, K1PROFI.Name, K1PROFI.Fields.EXEC, def: "");
            obj.CRAT = UIXQuery.GetDateTime(reader, K1PROFI.Name, K1PROFI.Fields.CRAT);
            obj.CHAT = UIXQuery.GetDateTime(reader, K1PROFI.Name, K1PROFI.Fields.CHAT);
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
            obj.State = UIXTableObjectState.Available;
            return obj;
        }
        #endregion

        #region Methods PRIVATE
        private void Insert(T1PROFI obj)
        {
            string sql = $"INSERT INTO T1PROFI (GANA, FIPL, LAPL, PPFN, EXGF, SAID, PRSE, EXEC, CRAT, CHAT, ACCO, ACIN, ACAC, CUPT, ETMA, ETME, ETCO, ETTY, ETML, ARCH, PLAFO) VALUES ({obj.GANA}, {obj.FIPL}, {obj.LAPL}, {obj.PPFN}, {obj.EXGF}, {obj.SAID}, {obj.PRSE}, {obj.EXEC}, {obj.CRAT}, {obj.CHAT}, {obj.ACCO}, {obj.ACIN}, {obj.ACAC}, {obj.CUPT}, {obj.ETMA}, {obj.ETME}, {obj.ETCO}, {obj.ETTY}, {obj.ETML}, {obj.ARCH}, {obj.PLAFO});";
            UIXQuery.ExecuteCustom(sql, _connection);

            using SQLiteCommand idCmd = _connection.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid();";
            obj.PFID = Convert.ToInt64(idCmd.ExecuteScalar());
        }

        private void Update(T1PROFI obj)
        {
            string sql = $"UPDATE T1PROFI SET GANA = '{obj.GANA}', FIPL = '{obj.FIPL}', LAPL = '{obj.LAPL}', PPFN = '{obj.PPFN}', EXGF = '{obj.EXGF}', SAID = '{obj.SAID}', PRSE = '{obj.PRSE}', EXEC = '{obj.EXEC}', CRAT = '{obj.CRAT}', CHAT = '{obj.CHAT}', ACCO = '{obj.ACCO}', ACIN = '{obj.ACIN}', ACAC = '{obj.ACAC}', CUPT = '{obj.CUPT}', ETMA = '{obj.ETMA}', ETME = '{obj.ETME}', ETCO = '{obj.ETCO}', ETTY = '{obj.ETTY}', ETML = '{obj.ETML}', ARCH = '{obj.ARCH}', PLAFO = '{obj.PLAFO}' WHERE PFID = '{obj.PFID}';";
            UIXQuery.ExecuteCustom(sql, _connection);
        }

        private bool Exists(T1PROFI obj)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM T1PROFI WHERE PFID = '{obj.PFID}';";
            return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }
        #endregion
    }
}
