using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase.Migration;
using System.Data.SQLite;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Migration.Types;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXGROUPBasic
    {
        protected SQLiteConnection _connection
        {
            get => AppEnvironment.GetDataBaseManager().GetConnection();
        }

        #region Methods PUBLIC
        public virtual T1GROUP CreateNew()
        {
            T1GROUP obj = new T1GROUP();
            obj.State = UIXTableObjectState.New;
            return obj;
        }

        public virtual void Save(T1GROUP obj)
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

        public virtual void Delete(long grid)
        {
            string sql = $"DELETE FROM T1GROUP WHERE GRID = '{grid}';";
            UIXQuery.ExecuteCustom(sql, _connection);
            MigrationFactory.ToCsv.ExportCsvFileFor(_connection, "T1GROUP", MigrationFactory.ImportType.DevSync);
        }

        public virtual T1GROUP? Read(long grid)
        {
            UIXQuery query = new UIXQuery(K1GROUP.Name, _connection);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.GRID);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.GRNA);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.GTYP);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.CRAT);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.CHAT);
            query.AddWhere(K1GROUP.Name, K1GROUP.Fields.GRID, QueryCompareType.EQUALS, grid);

            using var reader = query.Execute();
            if (!reader.Read())
            {
                return null;
            }

            T1GROUP obj = Map(reader);
            obj.AcceptChanges();
            return obj;
        }

        public virtual List<T1GROUP> ReadAll()
        {
            UIXQuery query = new UIXQuery(K1GROUP.Name, _connection);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.GRID);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.GRNA);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.GTYP);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.CRAT);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.CHAT);
            query.AddOrderBy(K1GROUP.Name, K1GROUP.Fields.GRID, OrderDirection.ASC);

            List<T1GROUP> list = new List<T1GROUP>();
            using var reader = query.Execute();
            while (reader.Read())
            {
                T1GROUP obj = Map(reader);
                obj.AcceptChanges();
                list.Add(obj);
            }
            return list;
        }
        #endregion

        #region Methods PRIVATE

        protected static T1GROUP Map(SQLiteDataReader reader)
        {
            T1GROUP obj = new T1GROUP();
            obj.GRID = UIXQuery.GetInt64(reader, K1GROUP.Name, K1GROUP.Fields.GRID);
            obj.GRNA = UIXQuery.GetString(reader, K1GROUP.Name, K1GROUP.Fields.GRNA, def: "");
            obj.GTYP = UIXQuery.GetString(reader, K1GROUP.Name, K1GROUP.Fields.GTYP, def: "");
            obj.CRAT = UIXQuery.GetDateTime(reader, K1GROUP.Name, K1GROUP.Fields.CRAT);
            obj.CHAT = UIXQuery.GetDateTime(reader, K1GROUP.Name, K1GROUP.Fields.CHAT);
            obj.State = UIXTableObjectState.Available;
            return obj;
        }
        #endregion

        #region Methods PRIVATE
        private void Insert(T1GROUP obj)
        {
            obj.CRAT = DateTime.Now;
            obj.CHAT = DateTime.Now;
            string sql = $"INSERT INTO T1GROUP (GRNA, GTYP, CRAT, CHAT) VALUES ('{ToDbValue(obj.GRNA)}', '{ToDbValue(obj.GTYP)}', '{ToDbValue(obj.CRAT)}', '{ToDbValue(obj.CHAT)}');";
            UIXQuery.ExecuteCustom(sql, _connection);

            using SQLiteCommand idCmd = _connection.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid();";
            obj.GRID = Convert.ToInt64(idCmd.ExecuteScalar());
        }

        private void Update(T1GROUP obj)
        {
            obj.CHAT = DateTime.Now;
            string sql = $"UPDATE T1GROUP SET GRNA = '{ToDbValue(obj.GRNA)}', GTYP = '{ToDbValue(obj.GTYP)}', CRAT = '{ToDbValue(obj.CRAT)}', CHAT = '{ToDbValue(obj.CHAT)}' WHERE GRID = '{obj.GRID}';";
            UIXQuery.ExecuteCustom(sql, _connection);
        }

        private bool Exists(T1GROUP obj)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM T1GROUP WHERE GRID = '{obj.GRID}';";
            return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        private static object? ToDbValue(object? valFrom)
        {
            return UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.CSharp, UIXSqliteDataType.ConversionType.Sqlite, valFrom);
        }
        #endregion
    }
}
