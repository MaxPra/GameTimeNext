using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase.Migration;
using System.Data.SQLite;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXCTABD : TXCTABDBasic
    {
        public void DeleteAllEntries(string txtyp)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "DELETE FROM T1CTABD " +
                "WHERE TXTYP = @TXTYP;";

            cmd.Parameters.AddWithValue("@TXTYP", txtyp);

            cmd.ExecuteNonQuery();

            MigrationFactory.ToCsv.ExportCsvFileFor(connection, "T1CTABD");
        }

        public List<T1CTABD> GetEntries(string txtyp)
        {
            SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();
            EnsureOpen(connection);

            List<T1CTABD> list = new List<T1CTABD>();

            using SQLiteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                "SELECT TXTYP, TXNUM, DESCR, PARM1, PARM2, CRAT, CHAT " +
                "FROM T1CTABD " +
                "WHERE TXTYP = @TXTYP " +
                "ORDER BY TXNUM;";

            cmd.Parameters.AddWithValue("@TXTYP", txtyp);

            using SQLiteDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                T1CTABD obj = Map(reader);
                obj.AcceptChanges();

                list.Add(obj);
            }

            return list;
        }
    }
}
