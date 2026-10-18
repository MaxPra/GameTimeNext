using System.Data.SQLite;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.Metadata.Data
{
    public class TXMETAP : TXMETAPBasic
    {
        public TXMETAP()
        {
        }

        public TXMETAP(SQLiteConnection connection) : base(connection)
        {
        }

        /// <summary>
        /// Reads all positions of a table, ordered by position order.
        /// </summary>
        public List<T1METAP> ReadAll(string menam)
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
            query.AddField(K1METAP.Name, K1METAP.Fields.CRAT);
            query.AddField(K1METAP.Name, K1METAP.Fields.CRUS);
            query.AddField(K1METAP.Name, K1METAP.Fields.CHAT);
            query.AddField(K1METAP.Name, K1METAP.Fields.CHUS);
            query.AddField(K1METAP.Name, K1METAP.Fields.DEFAK);
            query.AddField(K1METAP.Name, K1METAP.Fields.DEFVL);
            query.AddWhere(K1METAP.Name, K1METAP.Fields.MENAM, QueryCompareType.EQUALS, menam);
            query.AddOrderBy(K1METAP.Name, K1METAP.Fields.PORDE, OrderDirection.ASC);
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
    }
}
