using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TFPLTHR
    {
        public static long GetCurrentPlaythroughPtid(long pfid)
        {

            long maxPTID = 0;

            UIXQuery query = new UIXQuery(K1PLTHR.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddAggregate(UIXQuery.AggregateFunc.MAX, K1PLTHR.Name, K1PLTHR.Fields.PTID, alias: "K1PLTHR_MAX_PTID");

            query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PFID, QueryCompareType.EQUALS, pfid);
            query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PTCO, QueryCompareType.EQUALS, false);

            using (var reader = query.Execute())
            {
                if (reader.Read())
                {
                    maxPTID = UIXQuery.GetInt64(reader, "K1PLTHR_MAX_PTID");
                }
            }

            return maxPTID;
        }

        public static T1PLTHR GetCurrentPlaythrough(long pfid)
        {
            return new TXPLTHR().Read(GetCurrentPlaythroughPtid(pfid));
        }

        public static bool HasCurrentPlaythrough(long pfid)
        {
            T1PLTHR currT1plthr = GetCurrentPlaythrough(pfid);

            return currT1plthr != null && currT1plthr.PTCO == false;
        }

        public static long GetCurrentPlaythroughCount(long pfid, string type)
        {
            long count = 0;

            List<string> types = new List<string>();

            // Wenn hier mit NEW_PLAYTHROUGH gesucht wird, muss natürlich auch der initiale Playthrough miteinbezogen werden
            if (type == PlaythroughType.NEW_PLAYTHROUGH)
            {
                types.Add(PlaythroughType.NEW_PLAYTHROUGH);
                types.Add(PlaythroughType.INITIAL_PLAYTHROUGH);
            }
            else
                types.Add(type);


            UIXQuery query = new UIXQuery(K1PLTHR.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddAggregate(UIXQuery.AggregateFunc.COUNT, K1PLTHR.Name, K1PLTHR.Fields.PTID, alias: "K1PLTHR_COUNT");

            query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PFID, QueryCompareType.EQUALS, pfid);
            query.AddWhereIn(K1PLTHR.Name, K1PLTHR.Fields.PTTY, types);


            string sql = query.PreviewQuery();

            using (var reader = query.Execute())
            {
                if (reader.Read())
                {
                    count = UIXQuery.GetInt64(reader, "K1PLTHR_COUNT");
                }
            }

            return count;
        }

        /// <summary>
        /// Erstellt einen neuen Playthrough für das übergebene Profil (Neuanlage)
        /// </summary>
        /// <param name="pfid"></param>
        /// <param name="description"></param>
        public static long CreateNewPlaythrough(long pfid, string description = "Playthrough #1 (Initial)", string type = PlaythroughType.INITIAL_PLAYTHROUGH)
        {
            TXPLTHR txplthr = new TXPLTHR();
            T1PLTHR t1plthr = txplthr.CreateNew();

            t1plthr.PFID = pfid;
            t1plthr.PTDE = description;
            t1plthr.PTTY = type;

            txplthr.Save(t1plthr);

            return t1plthr.PTID;

        }

    }
}
