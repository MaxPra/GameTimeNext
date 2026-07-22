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

            UIXQuery query = BuildQueryLastSessionGameTime(pfid);

            using (var reader = query.Execute())
            {
                if (reader.Read())
                {
                    lastSessionPlaytime = UIXQuery.GetDouble(reader, K1SESSI.Name, K1SESSI.Fields.PLTI);
                }
            }

            return lastSessionPlaytime;
        }

        public static int GetInvalidSessionsCount()
        {
            UIXQuery query = BuildQueryInvalidSessionsCount();

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
            UIXQuery query = BuildQueryInvalidSessions();

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

        public static UIXQuery BuildQuerySessionsInTimeSpanBase(DateTime? start, DateTime end, long? pfid = null)
        {
            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            if (start is not null)
                query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PLTO, QueryCompareType.GREATER_OR_EQUAL, start.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PLFR, QueryCompareType.LESS_THAN, end.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            if (pfid is not null)
                query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PFID, QueryCompareType.EQUALS, pfid);

            return query;
        }

        private static UIXQuery BuildQueryLastSessionGameTime(long pfid)
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

        private static UIXQuery BuildQueryInvalidSessionsCount()
        {
            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddCountAll("Count");

            AddWhereInvalidSesssions(ref query);

            return query;
        }

        private static UIXQuery BuildQueryInvalidSessions()
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
