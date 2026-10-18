using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase.Migration;
using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Migration.Types;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXCTABHBasic
    {
        protected SQLiteConnection _connection
        {
            get => AppEnvironment.GetDataBaseManager().GetConnection();
        }

        #region Methods PUBLIC
        public virtual T1CTABH CreateNew()
        {
            T1CTABH obj = new T1CTABH();
            obj.State = UIXTableObjectState.New;
            return obj;
        }

        public virtual void Save(T1CTABH obj)
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

        public virtual void Delete(string txtyp)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM T1CTABH WHERE TXTYP = @TXTYP;";
            cmd.Parameters.AddWithValue("@TXTYP", ToDbValue(txtyp));
            cmd.ExecuteNonQuery();
            MigrationFactory.ToCsv.ExportCsvFileFor(_connection, "T1CTABH", MigrationFactory.ImportType.DevSync);
        }

        public virtual T1CTABH? Read(string txtyp)
        {
            UIXQuery query = new UIXQuery(K1CTABH.Name, _connection);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.TXTYP);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.DESCR);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PERMI);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PAAC1);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PADE1);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PARF1);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PACO1);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PACT1);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PAAC2);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PADE2);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PARF2);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PACO2);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PACT2);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.CRAT);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.CHAT);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.NRANA);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.EXPRT);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PTOL1);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PTOL2);
            query.AddWhere(K1CTABH.Name, K1CTABH.Fields.TXTYP, QueryCompareType.EQUALS, txtyp);

            using var reader = query.Execute();
            if (!reader.Read())
            {
                return null;
            }

            T1CTABH obj = Map(reader);
            obj.AcceptChanges();
            return obj;
        }

        public virtual List<T1CTABH> ReadAll()
        {
            UIXQuery query = new UIXQuery(K1CTABH.Name, _connection);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.TXTYP);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.DESCR);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PERMI);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PAAC1);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PADE1);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PARF1);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PACO1);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PACT1);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PAAC2);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PADE2);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PARF2);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PACO2);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PACT2);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.CRAT);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.CHAT);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.NRANA);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.EXPRT);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PTOL1);
            query.AddField(K1CTABH.Name, K1CTABH.Fields.PTOL2);
            query.AddOrderBy(K1CTABH.Name, K1CTABH.Fields.TXTYP, OrderDirection.ASC);

            List<T1CTABH> list = new List<T1CTABH>();
            using var reader = query.Execute();
            while (reader.Read())
            {
                T1CTABH obj = Map(reader);
                obj.AcceptChanges();
                list.Add(obj);
            }
            return list;
        }
        #endregion

        #region Methods PRIVATE

        protected static T1CTABH Map(SQLiteDataReader reader)
        {
            T1CTABH obj = new T1CTABH();
            obj.TXTYP = UIXQuery.GetString(reader, K1CTABH.Name, K1CTABH.Fields.TXTYP, def: "");
            obj.DESCR = UIXQuery.GetString(reader, K1CTABH.Name, K1CTABH.Fields.DESCR, def: "");
            obj.PERMI = UIXQuery.GetString(reader, K1CTABH.Name, K1CTABH.Fields.PERMI, def: "");
            obj.PAAC1 = UIXQuery.GetBool(reader, K1CTABH.Name, K1CTABH.Fields.PAAC1);
            obj.PADE1 = UIXQuery.GetString(reader, K1CTABH.Name, K1CTABH.Fields.PADE1, def: "");
            obj.PARF1 = UIXQuery.GetBool(reader, K1CTABH.Name, K1CTABH.Fields.PARF1);
            obj.PACO1 = UIXQuery.GetString(reader, K1CTABH.Name, K1CTABH.Fields.PACO1, def: "");
            obj.PACT1 = UIXQuery.GetString(reader, K1CTABH.Name, K1CTABH.Fields.PACT1, def: "");
            obj.PAAC2 = UIXQuery.GetBool(reader, K1CTABH.Name, K1CTABH.Fields.PAAC2);
            obj.PADE2 = UIXQuery.GetString(reader, K1CTABH.Name, K1CTABH.Fields.PADE2, def: "");
            obj.PARF2 = UIXQuery.GetBool(reader, K1CTABH.Name, K1CTABH.Fields.PARF2);
            obj.PACO2 = UIXQuery.GetString(reader, K1CTABH.Name, K1CTABH.Fields.PACO2, def: "");
            obj.PACT2 = UIXQuery.GetString(reader, K1CTABH.Name, K1CTABH.Fields.PACT2, def: "");
            obj.CRAT = UIXQuery.GetDateTime(reader, K1CTABH.Name, K1CTABH.Fields.CRAT);
            obj.CHAT = UIXQuery.GetDateTime(reader, K1CTABH.Name, K1CTABH.Fields.CHAT);
            obj.NRANA = UIXQuery.GetBool(reader, K1CTABH.Name, K1CTABH.Fields.NRANA);
            obj.EXPRT = UIXQuery.GetBool(reader, K1CTABH.Name, K1CTABH.Fields.EXPRT);
            obj.PTOL1 = UIXQuery.GetString(reader, K1CTABH.Name, K1CTABH.Fields.PTOL1, def: "");
            obj.PTOL2 = UIXQuery.GetString(reader, K1CTABH.Name, K1CTABH.Fields.PTOL2, def: "");
            obj.State = UIXTableObjectState.Available;
            return obj;
        }
        #endregion

        #region Methods PRIVATE
        private void Insert(T1CTABH obj)
        {
            obj.CRAT = DateTime.Now;
            obj.CHAT = DateTime.Now;
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO T1CTABH (TXTYP, DESCR, PERMI, PAAC1, PADE1, PARF1, PACO1, PACT1, PAAC2, PADE2, PARF2, PACO2, PACT2, CRAT, CHAT, NRANA, EXPRT, PTOL1, PTOL2) VALUES (@TXTYP, @DESCR, @PERMI, @PAAC1, @PADE1, @PARF1, @PACO1, @PACT1, @PAAC2, @PADE2, @PARF2, @PACO2, @PACT2, @CRAT, @CHAT, @NRANA, @EXPRT, @PTOL1, @PTOL2);";
            cmd.Parameters.AddWithValue("@TXTYP", ToDbValue(obj.TXTYP));
            cmd.Parameters.AddWithValue("@DESCR", ToDbValue(obj.DESCR));
            cmd.Parameters.AddWithValue("@PERMI", ToDbValue(obj.PERMI));
            cmd.Parameters.AddWithValue("@PAAC1", ToDbValue(obj.PAAC1));
            cmd.Parameters.AddWithValue("@PADE1", ToDbValue(obj.PADE1));
            cmd.Parameters.AddWithValue("@PARF1", ToDbValue(obj.PARF1));
            cmd.Parameters.AddWithValue("@PACO1", ToDbValue(obj.PACO1));
            cmd.Parameters.AddWithValue("@PACT1", ToDbValue(obj.PACT1));
            cmd.Parameters.AddWithValue("@PAAC2", ToDbValue(obj.PAAC2));
            cmd.Parameters.AddWithValue("@PADE2", ToDbValue(obj.PADE2));
            cmd.Parameters.AddWithValue("@PARF2", ToDbValue(obj.PARF2));
            cmd.Parameters.AddWithValue("@PACO2", ToDbValue(obj.PACO2));
            cmd.Parameters.AddWithValue("@PACT2", ToDbValue(obj.PACT2));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@NRANA", ToDbValue(obj.NRANA));
            cmd.Parameters.AddWithValue("@EXPRT", ToDbValue(obj.EXPRT));
            cmd.Parameters.AddWithValue("@PTOL1", ToDbValue(obj.PTOL1));
            cmd.Parameters.AddWithValue("@PTOL2", ToDbValue(obj.PTOL2));
            cmd.ExecuteNonQuery();
        }

        private void Update(T1CTABH obj)
        {
            obj.CHAT = DateTime.Now;
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE T1CTABH SET TXTYP = @TXTYP, DESCR = @DESCR, PERMI = @PERMI, PAAC1 = @PAAC1, PADE1 = @PADE1, PARF1 = @PARF1, PACO1 = @PACO1, PACT1 = @PACT1, PAAC2 = @PAAC2, PADE2 = @PADE2, PARF2 = @PARF2, PACO2 = @PACO2, PACT2 = @PACT2, CRAT = @CRAT, CHAT = @CHAT, NRANA = @NRANA, EXPRT = @EXPRT, PTOL1 = @PTOL1, PTOL2 = @PTOL2 WHERE TXTYP = @TXTYP;";
            cmd.Parameters.AddWithValue("@TXTYP", ToDbValue(obj.TXTYP));
            cmd.Parameters.AddWithValue("@DESCR", ToDbValue(obj.DESCR));
            cmd.Parameters.AddWithValue("@PERMI", ToDbValue(obj.PERMI));
            cmd.Parameters.AddWithValue("@PAAC1", ToDbValue(obj.PAAC1));
            cmd.Parameters.AddWithValue("@PADE1", ToDbValue(obj.PADE1));
            cmd.Parameters.AddWithValue("@PARF1", ToDbValue(obj.PARF1));
            cmd.Parameters.AddWithValue("@PACO1", ToDbValue(obj.PACO1));
            cmd.Parameters.AddWithValue("@PACT1", ToDbValue(obj.PACT1));
            cmd.Parameters.AddWithValue("@PAAC2", ToDbValue(obj.PAAC2));
            cmd.Parameters.AddWithValue("@PADE2", ToDbValue(obj.PADE2));
            cmd.Parameters.AddWithValue("@PARF2", ToDbValue(obj.PARF2));
            cmd.Parameters.AddWithValue("@PACO2", ToDbValue(obj.PACO2));
            cmd.Parameters.AddWithValue("@PACT2", ToDbValue(obj.PACT2));
            cmd.Parameters.AddWithValue("@CRAT", ToDbValue(obj.CRAT));
            cmd.Parameters.AddWithValue("@CHAT", ToDbValue(obj.CHAT));
            cmd.Parameters.AddWithValue("@NRANA", ToDbValue(obj.NRANA));
            cmd.Parameters.AddWithValue("@EXPRT", ToDbValue(obj.EXPRT));
            cmd.Parameters.AddWithValue("@PTOL1", ToDbValue(obj.PTOL1));
            cmd.Parameters.AddWithValue("@PTOL2", ToDbValue(obj.PTOL2));
            cmd.ExecuteNonQuery();
        }

        private bool Exists(T1CTABH obj)
        {
            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM T1CTABH WHERE TXTYP = @TXTYP;";
            cmd.Parameters.AddWithValue("@TXTYP", ToDbValue(obj.TXTYP));
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        private static object? ToDbValue(object? valFrom)
        {
            return UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.CSharp, UIXSqliteDataType.ConversionType.Sqlite, valFrom);
        }
        #endregion
    }
}
