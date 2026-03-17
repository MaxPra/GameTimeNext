using GameTimeNext.Core.Application.Profiles.Components;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TFPROFI
    {
        /// <summary>
        /// Löscht das übergebene Profil inkl. der zugehörigen Daten
        /// </summary>
        public static void DeleteProfiAndLinkedData(T1PROFI t1profi)
        {
            TXPROFI txprofi = new TXPROFI();

            // Zugehörige Daten löschen
            TXGRPPO txgrppo = new TXGRPPO();
            List<T1GRPPO> t1grppos = txgrppo.ReadAll();

            t1grppos = t1grppos.Where(g => g.PFID == t1profi.PFID).ToList();

            foreach (T1GRPPO t1grppo in t1grppos)
            {
                txgrppo.Delete(t1grppo.GPID);
            }

            // Profil löschen
            txprofi.Delete(t1profi.PFID);
        }

        public static bool GetIsUnplayed(T1PROFI t1profi)
        {
            return t1profi.FIPL == DateTime.MinValue;
        }

        public static bool HasExecutables(T1PROFI t1profi)
        {
            CExecutables cExecutables = new CExecutables(t1profi.EXEC).Dezerialize();

            if (cExecutables.KeyValuePairs.Count == 0)
                return false;

            return true;
        }

        public static List<T1GROUP> GetAllLinkedTags(T1PROFI t1profi)
        {

            List<T1GROUP> tags = new List<T1GROUP>();

            UIXQuery query = BuildLinkedTagsQuery(t1profi.PFID);

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    long grid = UIXQuery.GetInt64(reader, K1GRPPO.Name, K1GRPPO.Fields.GRID);

                    tags.Add(new TXGROUP().Read(grid));
                }
            }

            return tags;
        }

        public static List<T1SESSI> GetAllSessions(T1PROFI t1profi)
        {
            List<T1SESSI> t1sessis = new List<T1SESSI>();

            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1SESSI.Name, K1SESSI.Fields.SEID);

            query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PFID, QueryCompareType.EQUALS, t1profi.PFID);

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    long seid = UIXQuery.GetInt64(reader, K1SESSI.Name, K1SESSI.Fields.SEID);
                    t1sessis.Add(new TXSESSI().Read(seid));
                }
            }

            return t1sessis;
        }

        public static double GetTodaysGameTimeInMinutes(long pfid)
        {
            UIXQuery query = BuildQueryTodaysGameTime(pfid);

            double playedMinutesToday = 0;

            string sql = query.PreviewQuery();
            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    DateTime plfr = UIXQuery.GetDateTime(reader, K1SESSI.Name, K1SESSI.Fields.PLFR);
                    DateTime plto = UIXQuery.GetDateTime(reader, K1SESSI.Name, K1SESSI.Fields.PLTO);
                    double plti = UIXQuery.GetDouble(reader, K1SESSI.Name, K1SESSI.Fields.PLTI);

                    // Wenn Von gleich gestern
                    // dann muss die differenz vom letzten Ende der Session zu 00:00 Uhr heute berechnet werden
                    if (plfr.Date == DateTime.Today.AddDays(-1))
                    {
                        playedMinutesToday += ((double)(plto - DateTime.Today).TotalSeconds / 60);
                    }
                    else
                    {
                        playedMinutesToday += plti;
                    }
                }
            }

            return playedMinutesToday;
        }

        private static UIXQuery BuildQueryTodaysGameTime(long pfid)
        {
            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1SESSI.Name, K1SESSI.Fields.PLFR);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PLTO);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PLTI);

            query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PFID, QueryCompareType.EQUALS, pfid);
            query.AddWhereDateOnDay(K1SESSI.Name, K1SESSI.Fields.PLTO, DateTime.Today);

            return query;
        }

        private static UIXQuery BuildLinkedTagsQuery(long pfid)
        {
            UIXQuery query = new UIXQuery(K1GRPPO.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            // Felder hinzufügen
            query.AddField(K1GRPPO.Name, K1GRPPO.Fields.GRID);

            // Where Restriktionen
            query.AddWhere(K1GRPPO.Name, K1GRPPO.Fields.PFID, QueryCompareType.EQUALS, pfid);

            return query;
        }
    }
}
