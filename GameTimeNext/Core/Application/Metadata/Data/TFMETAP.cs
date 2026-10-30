using GameTimeNext.Core.Framework;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.Metadata.Data
{
    public class TFMETAP
    {
        public static int GetNextOrder(T1METAP t1metap)
        {
            UIXQuery query = BuildQueryMaxOrder(t1metap);

            using (var reader = query.Execute())
            {
                if (reader.Read())
                    return UIXQuery.GetInt32(reader, "MAX_PORDE") + 10;
            }

            return 1;
        }

        public static readonly string[] AuditFields = { "CRAT", "CRUS", "CHAT", "CHUS" };

        public static bool AutoReorder(string menam)
        {
            TXMETAP txmetap = new TXMETAP();
            List<T1METAP> t1metaps = txmetap.ReadAll(menam);

            IEnumerable<T1METAP> fields = t1metaps.Where(p => !AuditFields.Contains(p.PONAM, StringComparer.Ordinal));
            IEnumerable<T1METAP> auditFields = AuditFields
                .Select(field => t1metaps.FirstOrDefault(p => string.Equals(p.PONAM, field, StringComparison.Ordinal)))
                .OfType<T1METAP>();

            bool hasChanged = false;
            int order = 10;

            foreach (T1METAP t1metap in fields.Concat(auditFields))
            {
                if (t1metap.PORDE != order)
                {
                    t1metap.PORDE = order;
                    txmetap.Save(t1metap);
                    hasChanged = true;
                }

                order += 10;
            }

            return hasChanged;
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
