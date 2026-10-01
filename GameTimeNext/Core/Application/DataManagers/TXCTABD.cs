using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.DataBase.Migration;
using System.Data.SQLite;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXCTABD : TXCTABDBasic
    {
        public void DeleteAllEntries(string txtyp)
        {
            string sql = $"DELETE FROM T1CTABD WHERE TXTYP = '{txtyp}';";
            UIXQuery.ExecuteCustom(sql, _connection);

            MigrationFactory.ToCsv.ExportCsvFileFor(_connection, "T1CTABD", MigrationFactory.ImportType.DevSync);
        }

        public List<T1CTABD> GetEntries(string txtyp)
        {
            List<T1CTABD> list = new List<T1CTABD>();

            UIXQuery query = new UIXQuery(K1CTABD.Name, _connection);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.TXTYP);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.TXNUM);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.DESCR);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.CRAT);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.CHAT);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.PARM1);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.PARM2);

            query.AddWhere(K1CTABD.Name, K1CTABD.Fields.TXTYP, QueryCompareType.EQUALS, txtyp);

            query.AddOrderBy(K1CTABD.Name, K1CTABD.Fields.TXTYP, OrderDirection.ASC);
            query.AddOrderBy(K1CTABD.Name, K1CTABD.Fields.TXNUM, OrderDirection.ASC);

            SQLiteDataReader reader = query.Execute();
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
