using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using System.Globalization;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TFSESSI
    {
        /// <summary>
        /// Ermittelt die Spielzeit der letzten Session
        /// </summary>
        /// <param name="pfid"></param>
        /// <returns></returns>
        public static double GetLastSessionGameTime(long pfid)
        {
            double lastSessionPlaytime = 0;

            UIXQuery query = BuildLastSessionGameTimeQuery(pfid);

            string sql = query.PreviewQuery();

            using (var reader = query.Execute())
            {
                if (reader.Read())
                {
                    lastSessionPlaytime = UIXQuery.GetDouble(reader, K1SESSI.Name, K1SESSI.Fields.PLTI);
                }
            }

            return lastSessionPlaytime;
        }

        /// <summary>
        /// Übermittelt die gespielten Tage für das übergebene Profil
        /// </summary>
        /// <param name="pfid"></param>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public static int GetPlayedDays(long pfid, long timeSpan)
        {
            int days = 0;

            UIXQuery query = BuildPlayedDaysQuery(pfid, timeSpan);

            string s = query.PreviewQuery();

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    days++;
                }
            }

            return days;
        }

        /// <summary>
        /// Ermittelt die gespielten Tage über alle Profile hinweg
        /// </summary>
        /// <returns></returns>
        public static int GetPlayedDays(long timeSpan)
        {
            return GetPlayedDays(0, timeSpan);
        }

        private static UIXQuery BuildPlayedDaysQuery(long pfid, long timeSpan)
        {
            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddFieldRaw("DATE(T1SESSI.PLFR)", "PLAYDAY");

            if (pfid > 0)
                query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PFID, QueryCompareType.EQUALS, pfid);

            if (timeSpan != 0)
            {
                DateTime limitDate = DateTime.Now.AddDays(-timeSpan);
                //query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PLFR, QueryCompareType.GREATER_THAN, limitDate);
                query.AddWhereRaw("DATE(T1SESSI.PLFR) > ?", limitDate.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }

            query.SetDistinct(true);

            return query;
        }

        private static UIXQuery BuildLastSessionGameTimeQuery(long pfid)
        {
            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            // Nur ein Datensatz
            query.SetTopX(1);

            // Felder
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PLTI);

            // Where
            query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PFID, QueryCompareType.EQUALS, pfid);

            // Order by
            query.AddOrderBy(K1SESSI.Name, K1SESSI.Fields.SEID, OrderDirection.DESC);

            return query;
        }
    }
}
