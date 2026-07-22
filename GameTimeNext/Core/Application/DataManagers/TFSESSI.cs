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
        public static int GetPlayedDays(long pfid, DateTime? timeSpanStart, DateTime timeSpanEnd)
        {
            int days = 0;

            UIXQuery query = BuildPlayedDaysQuery(pfid, timeSpanStart, timeSpanEnd);

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    days++;
                }
            }

            return days;
        }

        public static int GetInvalidSessionsCount()
        {
            UIXQuery query = BuildInvalidSessionsCountQuery();

            int count = 0;
            using (var reader = query.Execute())
            {
                reader.Read();
                count = UIXQuery.GetInt32(reader, "Count");
            }

            return count;
        }

        public static void CleanupInvalidSessions()
        {
            UIXQuery query = BuildInvalidSessionsQuery();

            using (var reader = query.Execute())
            {
                TXSESSI txSessi = new TXSESSI();
                while(reader.Read())
                {
                    int seid = UIXQuery.GetInt32(reader, K1SESSI.Name, K1SESSI.Fields.SEID);
                    txSessi.Delete(seid);
                }
            }
        }

        private static UIXQuery BuildPlayedDaysQuery(long pfid, DateTime? timeSpanStart, DateTime timeSpanEnd)
        {
            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddFieldRaw("DATE(T1SESSI.PLFR)", "PLAYDAY");

            if (pfid > 0)
                query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PFID, QueryCompareType.EQUALS, pfid);

            if (timeSpanStart is not null)
                query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PLFR, QueryCompareType.GREATER_OR_EQUAL, timeSpanStart.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            
            query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PLFR, QueryCompareType.LESS_OR_EQUAL, timeSpanEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

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
            query.AddOrderBy(K1SESSI.Name, K1SESSI.Fields.PLTO, OrderDirection.DESC);

            return query;
        }

        private static UIXQuery BuildInvalidSessionsCountQuery()
        {
            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddCountAll("Count");

            AddWhereInvalidSesssions(ref query);

            return query;
        }

        private static UIXQuery BuildInvalidSessionsQuery()
        {
            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1SESSI.Name, K1SESSI.Fields.SEID);

            AddWhereInvalidSesssions(ref query);

            return query;
        }

        private static void AddWhereInvalidSesssions(ref UIXQuery query)
        {
            query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PLTI, QueryCompareType.LESS_THAN, AppEnvironment.GetAppConfig().AppSettings.SessionCleanupSeconds / 60);
        }
    }
}
