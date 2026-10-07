using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase.Migration;
using System.Data.SQLite;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Migration.Types;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXSESSIBasic
    {
        protected SQLiteConnection _connection
        {
            get => AppEnvironment.GetDataBaseManager().GetConnection();
        }

        #region Methods PUBLIC
        public virtual T1SESSI CreateNew()
        {
            T1SESSI obj = new T1SESSI();
            obj.State = UIXTableObjectState.New;
            return obj;
        }

        public virtual void Save(T1SESSI obj)
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

        public virtual void Delete(long seid)
        {
            string sql = $"DELETE FROM T1SESSI WHERE SEID = '{seid}';";
            UIXQuery.ExecuteCustom(sql, _connection);
            MigrationFactory.ToCsv.ExportCsvFileFor(_connection, "T1SESSI", MigrationFactory.ImportType.DevSync);
        }

        public virtual T1SESSI? Read(long seid)
        {
            UIXQuery query = new UIXQuery(K1SESSI.Name, _connection);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.SEID);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PFID);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PTID);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PLFR);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PLTO);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PLTI);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.CRAT);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.CHAT);
            query.AddWhere(K1SESSI.Name, K1SESSI.Fields.SEID, QueryCompareType.EQUALS, seid);

            using var reader = query.Execute();
            if (!reader.Read())
            {
                return null;
            }

            T1SESSI obj = Map(reader);
            obj.AcceptChanges();
            return obj;
        }

        public virtual List<T1SESSI> ReadAll()
        {
            UIXQuery query = new UIXQuery(K1SESSI.Name, _connection);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.SEID);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PFID);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PTID);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PLFR);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PLTO);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PLTI);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.CRAT);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.CHAT);
            query.AddOrderBy(K1SESSI.Name, K1SESSI.Fields.SEID, OrderDirection.ASC);

            List<T1SESSI> list = new List<T1SESSI>();
            using var reader = query.Execute();
            while (reader.Read())
            {
                T1SESSI obj = Map(reader);
                obj.AcceptChanges();
                list.Add(obj);
            }
            return list;
        }
        #endregion

        #region Methods PRIVATE

        protected static T1SESSI Map(SQLiteDataReader reader)
        {
            T1SESSI obj = new T1SESSI();
            obj.SEID = UIXQuery.GetInt64(reader, K1SESSI.Name, K1SESSI.Fields.SEID);
            obj.PFID = UIXQuery.GetInt64(reader, K1SESSI.Name, K1SESSI.Fields.PFID);
            obj.PTID = UIXQuery.GetInt64(reader, K1SESSI.Name, K1SESSI.Fields.PTID);
            obj.PLFR = UIXQuery.GetDateTime(reader, K1SESSI.Name, K1SESSI.Fields.PLFR);
            obj.PLTO = UIXQuery.GetDateTime(reader, K1SESSI.Name, K1SESSI.Fields.PLTO);
            obj.PLTI = UIXQuery.GetDouble(reader, K1SESSI.Name, K1SESSI.Fields.PLTI);
            obj.CRAT = UIXQuery.GetDateTime(reader, K1SESSI.Name, K1SESSI.Fields.CRAT);
            obj.CHAT = UIXQuery.GetDateTime(reader, K1SESSI.Name, K1SESSI.Fields.CHAT);
            obj.State = UIXTableObjectState.Available;
            return obj;
        }
        #endregion

        #region Methods PRIVATE
        private void Insert(T1SESSI obj)
        {
            obj.CRAT = DateTime.Now;
            obj.CHAT = DateTime.Now;
            string sql = $"INSERT INTO T1SESSI (PFID, PTID, PLFR, PLTO, PLTI, CRAT, CHAT) VALUES ('{ToDbValue(obj.PFID)}', '{ToDbValue(obj.PTID)}', '{ToDbValue(obj.PLFR)}', '{ToDbValue(obj.PLTO)}', '{ToDbValue(obj.PLTI)}', '{ToDbValue(obj.CRAT)}', '{ToDbValue(obj.CHAT)}');";
            UIXQuery.ExecuteCustom(sql, _connection);

            using SQLiteCommand idCmd = _connection.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid();";
            obj.SEID = Convert.ToInt64(idCmd.ExecuteScalar());
        }

        private void Update(T1SESSI obj)
        {
            obj.CHAT = DateTime.Now;
            string sql = $"UPDATE T1SESSI SET PFID = '{ToDbValue(obj.PFID)}', PTID = '{ToDbValue(obj.PTID)}', PLFR = '{ToDbValue(obj.PLFR)}', PLTO = '{ToDbValue(obj.PLTO)}', PLTI = '{ToDbValue(obj.PLTI)}', CRAT = '{ToDbValue(obj.CRAT)}', CHAT = '{ToDbValue(obj.CHAT)}' WHERE SEID = '{obj.SEID}';";
            UIXQuery.ExecuteCustom(sql, _connection);
        }

        private bool Exists(T1SESSI obj)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM T1SESSI WHERE SEID = '{obj.SEID}';";
            return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        private static object? ToDbValue(object? valFrom)
        {
            return UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.CSharp, UIXSqliteDataType.ConversionType.Sqlite, valFrom);
        }
        #endregion
    }
}
