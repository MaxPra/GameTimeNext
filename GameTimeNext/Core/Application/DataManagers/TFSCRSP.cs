using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TFSCRSP
    {
        public static List<T1SCRSP> GetMonitorsOfScreensetup(long ssid)
        {
            List<T1SCRSP> t1scrsps = new List<T1SCRSP>();

            if (ssid == 0)
                return t1scrsps;

            UIXQuery query = new UIXQuery(K1SCRSP.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddField(K1SCRSP.Name, K1SCRSP.Fields.SPID);
            query.AddWhere(K1SCRSP.Name, K1SCRSP.Fields.SSID, QueryCompareType.EQUALS, ssid);
            query.AddOrderBy(K1SCRSP.Name, K1SCRSP.Fields.SPID, OrderDirection.ASC);

            TXSCRSP txscrsp = new TXSCRSP();

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    long spid = UIXQuery.GetInt64(reader, K1SCRSP.Name, K1SCRSP.Fields.SPID);

                    T1SCRSP? t1scrsp = txscrsp.Read(spid);

                    if (t1scrsp != null)
                        t1scrsps.Add(t1scrsp);
                }
            }

            return t1scrsps;
        }
    }
}
