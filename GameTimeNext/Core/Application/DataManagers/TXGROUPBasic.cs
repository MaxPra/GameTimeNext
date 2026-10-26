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
    public class TXGROUPBasic
    {
        private readonly SQLiteConnection? _connectionOverride;

        protected SQLiteConnection _connection
        {
            get => _connectionOverride ?? AppEnvironment.GetDataBaseManager().GetConnection();
        }

        public TXGROUPBasic()
        {
        }

        public TXGROUPBasic(SQLiteConnection connection)
        {
            _connectionOverride = connection;
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
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM T1GROUP WHERE GRID = @GRID;";
            cmd.Parameters.AddWithValue("@GRID", ToDbValue(grid));
            cmd.ExecuteNonQuery();
            MigrationFactory.ToCsv.ExportCsvFileFor(_connection, "T1GROUP", MigrationFactory.ImportType.DevSync);
        }

        public bool Exists(T1GROUP obj)
        {
            return Exists(obj.GRID);
        }

        public bool Exists(long grid)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM T1GROUP WHERE GRID = @GRID;";
            cmd.Parameters.AddWithValue("@GRID", ToDbValue(grid));
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        public virtual T1GROUP? Read(long grid)
        {
            UIXQuery query = new UIXQuery(K1GROUP.Name, _connection);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.GRID);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.GRNA);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.GTYP);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.CRAT);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.CRUS);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.CHAT);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.CHUS);
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
            query.AddField(K1GROUP.Name, K1GROUP.Fields.CRUS);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.CHAT);
            query.AddField(K1GROUP.Name, K1GROUP.Fields.CHUS);
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

        #region Methods PROTECTED

        protected static T1GROUP Map(SQLiteDataReader reader)
        {
            T1GROUP obj = new T1GROUP();
            obj.GRID = UIXQuery.GetInt64(reader, K1GROUP.Name, K1GROUP.Fields.GRID);
            obj.GRNA = UIXQuery.GetString(reader, K1GROUP.Name, K1GROUP.Fields.GRNA, def: "");
            obj.GTYP = UIXQuery.GetString(reader, K1GROUP.Name, K1GROUP.Fields.GTYP, def: "");
            obj.CRAT = UIXQuery.GetDateTime(reader, K1GROUP.Name, K1GROUP.Fields.CRAT);
            obj.CRUS = UIXQuery.GetString(reader, K1GROUP.Name, K1GROUP.Fields.CRUS, def: "");
            obj.CHAT = UIXQuery.GetDateTime(reader, K1GROUP.Name, K1GROUP.Fields.CHAT);
            obj.CHUS = UIXQuery.GetString(reader, K1GROUP.Name, K1GROUP.Fields.CHUS, def: "");
            obj.State = UIXTableObjectState.Available;
            return obj;
        }
        #endregion

        #region Methods PRIVATE
        private void Insert(T1GROUP obj)
        {
            obj.CRAT = DateTime.Now;
            obj.CHAT = DateTime.Now;
            obj.CRUS = FnOpSys.GetCurrentUserName();
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO T1GROUP (GRNA, GTYP, CRAT, CRUS, CHAT, CHUS) VALUES (@GRNA, @GTYP, @CRAT, @CRUS, @CHAT, @CHUS);";
            cmd.Parameters.AddWithValue("@GRNA", ToDbValue(obj.GRNA));
            cmd.Parameters.AddWithValue("@GTYP", ToDbValue(obj.GTYP));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CRUS", ToDbValue(obj.CRUS));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@CHUS", ToDbValue(obj.CHUS));
            cmd.ExecuteNonQuery();

            using SQLiteCommand idCmd = _connection.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid();";
            obj.GRID = Convert.ToInt64(idCmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private void Update(T1GROUP obj)
        {
            obj.CHAT = DateTime.Now;
            obj.CHUS = FnOpSys.GetCurrentUserName();
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE T1GROUP SET GRNA = @GRNA, GTYP = @GTYP, CRAT = @CRAT, CRUS = @CRUS, CHAT = @CHAT, CHUS = @CHUS WHERE GRID = @GRID;";
            cmd.Parameters.AddWithValue("@GRID", ToDbValue(obj.GRID));
            cmd.Parameters.AddWithValue("@GRNA", ToDbValue(obj.GRNA));
            cmd.Parameters.AddWithValue("@GTYP", ToDbValue(obj.GTYP));
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
