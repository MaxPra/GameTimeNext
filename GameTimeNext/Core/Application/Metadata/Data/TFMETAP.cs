using GameTimeNext.Core.Framework;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.Metadata.Data
{
    public class TFMETAP
    {
        public static int GetNextOrder(T1METAP t1metap)
        {
            UIXQuery query = BuildQueryMaxOrder(t1metap);

            string s = query.PreviewQuery();

            using (var reader = query.Execute())
            {
                if (reader.Read())
                    return UIXQuery.GetInt32(reader, "MAX_PORDE") + 1;
            }

            return 1;
        }

        private static UIXQuery BuildQueryMaxOrder(T1METAP t1metap)
        {
            UIXQuery query = new UIXQuery(K1METAP.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddAggregate(UIXQuery.AggregateFunc.MAX, K1METAP.Name, K1METAP.Fields.PORDE, false, alias: "MAX_PORDE");

            query.AddWhere(K1METAP.Name, K1METAP.Fields.MENAM, QueryCompareType.EQUALS, t1metap.MENAM);

            return query;
        }
    }
}
