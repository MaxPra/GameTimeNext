using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase.Migration;
using System.Data.SQLite;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXPLTHRBasic
    {
        protected SQLiteConnection _connection
        {
            get => AppEnvironment.GetDataBaseManager().GetConnection();
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
            string sql = $"DELETE FROM T1PLTHR WHERE PTID = '{ptid}';";
            UIXQuery.ExecuteCustom(sql, _connection);
            MigrationFactory.ToCsv.ExportCsvFileFor(_connection, "T1PLTHR", MigrationFactory.ImportType.DevSync);
        }

        public virtual T1PLTHR? Read(long ptid)
        {
            UIXQuery query = new UIXQuery(K1PLTHR.Name, _connection);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTID);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PFID);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTTY);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTDE);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTCO);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.CRAT);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.CHAT);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTCA);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTPA);
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
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.CRAT);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.CHAT);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTCA);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTPA);
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

        #region Methods PRIVATE

        protected static T1PLTHR Map(SQLiteDataReader reader)
        {
            T1PLTHR obj = new T1PLTHR();
            obj.PTID = UIXQuery.GetInt64(reader, K1PLTHR.Name, K1PLTHR.Fields.PTID);
            obj.PFID = UIXQuery.GetInt64(reader, K1PLTHR.Name, K1PLTHR.Fields.PFID);
            obj.PTTY = UIXQuery.GetString(reader, K1PLTHR.Name, K1PLTHR.Fields.PTTY, def: "");
            obj.PTDE = UIXQuery.GetString(reader, K1PLTHR.Name, K1PLTHR.Fields.PTDE, def: "");
            obj.PTCO = UIXQuery.GetBool(reader, K1PLTHR.Name, K1PLTHR.Fields.PTCO);
            obj.CRAT = UIXQuery.GetDateTime(reader, K1PLTHR.Name, K1PLTHR.Fields.CRAT);
            obj.CHAT = UIXQuery.GetDateTime(reader, K1PLTHR.Name, K1PLTHR.Fields.CHAT);
            obj.PTCA = UIXQuery.GetBool(reader, K1PLTHR.Name, K1PLTHR.Fields.PTCA);
            obj.PTPA = UIXQuery.GetBool(reader, K1PLTHR.Name, K1PLTHR.Fields.PTPA);
            obj.State = UIXTableObjectState.Available;
            return obj;
        }
        #endregion

        #region Methods PRIVATE
        private void Insert(T1PLTHR obj)
        {
            string sql = $"INSERT INTO T1PLTHR (PFID, PTTY, PTDE, PTCO, CRAT, CHAT, PTCA, PTPA) VALUES ('{obj.PFID}', '{obj.PTTY}', '{obj.PTDE}', '{obj.PTCO}', '{obj.CRAT}', '{obj.CHAT}', '{obj.PTCA}', '{obj.PTPA}');";
            UIXQuery.ExecuteCustom(sql, _connection);

            using SQLiteCommand idCmd = _connection.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid();";
            obj.PTID = Convert.ToInt64(idCmd.ExecuteScalar());
        }

        private void Update(T1PLTHR obj)
        {
            string sql = $"UPDATE T1PLTHR SET PFID = '{obj.PFID}', PTTY = '{obj.PTTY}', PTDE = '{obj.PTDE}', PTCO = '{obj.PTCO}', CRAT = '{obj.CRAT}', CHAT = '{obj.CHAT}', PTCA = '{obj.PTCA}', PTPA = '{obj.PTPA}' WHERE PTID = '{obj.PTID}';";
            UIXQuery.ExecuteCustom(sql, _connection);
        }

        private bool Exists(T1PLTHR obj)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM T1PLTHR WHERE PTID = '{obj.PTID}';";
            return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }
        #endregion
    }
}
