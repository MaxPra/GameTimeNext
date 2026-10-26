using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.UI.Dialogs;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

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
            query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PTCA, QueryCompareType.EQUALS, false);
            query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PTPA, QueryCompareType.EQUALS, false);

            using (var reader = query.Execute())
            {
                if (reader.Read())
                {
                    maxPTID = UIXQuery.GetInt64(reader, "K1PLTHR_MAX_PTID");
                }
            }

            return maxPTID;
        }

        /// <summary>
        /// Returns the PFIDs of all profiles that have at least one completed playthrough
        /// </summary>
        public static HashSet<long> GetPfidsWithCompletedPlaythrough()
        {
            return GetPfidsWithPlaythrough(completed: true, cancelled: false, paused: false);
        }

        /// <summary>
        /// Returns the PFIDs of all profiles that have a current playthrough
        /// (not completed, cancelled or paused - same criteria as HasCurrentPlaythrough)
        /// </summary>
        public static HashSet<long> GetPfidsWithCurrentPlaythrough()
        {
            return GetPfidsWithPlaythrough(completed: false, cancelled: false, paused: false);
        }

        /// <summary>
        /// Returns the PFIDs of all profiles that have at least one paused playthrough
        /// </summary>
        public static HashSet<long> GetPfidsWithPausedPlaythrough()
        {
            return GetPfidsWithPlaythrough(completed: false, cancelled: false, paused: true);
        }

        private static HashSet<long> GetPfidsWithPlaythrough(bool completed, bool cancelled, bool paused)
        {
            HashSet<long> pfids = new HashSet<long>();

            UIXQuery query = new UIXQuery(K1PLTHR.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.SetDistinct(true);
            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PFID);
            query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PTCO, QueryCompareType.EQUALS, completed);

            // A completed playthrough counts regardless of the other flags
            if (!completed)
            {
                query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PTCA, QueryCompareType.EQUALS, cancelled);
                query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PTPA, QueryCompareType.EQUALS, paused);
            }

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    pfids.Add(UIXQuery.GetInt64(reader, K1PLTHR.Name, K1PLTHR.Fields.PFID));
                }
            }

            return pfids;
        }

        public static T1PLTHR GetCurrentPlaythrough(long pfid)
        {
            return new TXPLTHR().Read(GetCurrentPlaythroughPtid(pfid));
        }

        public static bool HasCurrentPlaythrough(long pfid)
        {
            T1PLTHR currT1plthr = GetCurrentPlaythrough(pfid);

            return currT1plthr != null && currT1plthr.PTCO == false && currT1plthr.PTCA == false && currT1plthr.PTPA == false;
        }

        public static long GetCurrentPlaythroughCount(long pfid, string type)
        {
            long count = 0;

            List<string> types = new List<string>();

            // When searching with NEW_PLAYTHROUGH, the initial playthrough has to be included as well
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
        /// Creates a new playthrough for the given profile (new entry)
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

        public static List<T1SESSI> GetSessions(long ptid)
        {
            List<T1SESSI> sessions = new List<T1SESSI>();
            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1SESSI.Name, K1SESSI.Fields.SEID);
            query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PTID, QueryCompareType.EQUALS, ptid);

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    long ssid = UIXQuery.GetInt64(reader, K1SESSI.Name, K1SESSI.Fields.SEID);

                    TXSESSI txsessi = new TXSESSI();
                    T1SESSI t1sessi = txsessi.Read(ssid);

                    sessions.Add(t1sessi);
                }
            }
            return sessions;
        }

        public static double GetPlaytimeInMinutes(long ptid)
        {
            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddSum(K1SESSI.Name, K1SESSI.Fields.PLTI, "TotalPlaytime");
            query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PTID, QueryCompareType.EQUALS, ptid);

            using (var reader = query.Execute())
                if (reader.Read())
                    return UIXQuery.GetDouble(reader, "TotalPlaytime");

            return 0;
        }

        public static void DeletePlaythroughAndSessions(long ptid)
        {
            TXPLTHR txplthr = new TXPLTHR();
            TXSESSI txsessi = new TXSESSI();

            List<T1SESSI> sessions = GetSessions(ptid);

            foreach (T1SESSI session in sessions)
            {
                txsessi.Delete(session.SEID);
            }

            txplthr.Delete(ptid);
        }

        public static void UpdatePlaythroughStatus(UIXApplication app, ref T1PLTHR t1plthr, PlaythroughStatus status, T1PLTHR? currentT1plthr = null)
        {
            string question = string.Empty;
            switch (status)
            {
                case PlaythroughStatus.Open:
                    if (!t1plthr.PTPA) break;

                    question = app.GetErrorMessage("PLT0002", "unpause");
                    if (currentT1plthr is not null)
                        question += "\n\n" + app.GetErrorMessage("PLT0004");
                    break;
                case PlaythroughStatus.Completed:
                    question = app.GetErrorMessage("PLT0003", "complete");
                    break;
                case PlaythroughStatus.Canceled:
                    question = app.GetErrorMessage("PLT0003", "cancel") + "\n\n" + app.GetErrorMessage("PLT0005");
                    break;
                case PlaythroughStatus.Paused:
                    question = app.GetErrorMessage("PLT0002", "pause");
                    break;
            }

            if (!FnString.IsNullEmptyOrWhitespace(question))
            {
                CFMBOXResult result = app.GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(question, CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);
                if (result == CFMBOXResult.No) return;
            }

            switch (status)
            {
                case PlaythroughStatus.Open:
                    t1plthr.PTCO = false;
                    t1plthr.PTCA = false;
                    t1plthr.PTPA = false;

                    // Pause current playthrough
                    if (currentT1plthr is not null)
                    {
                        currentT1plthr.PTCO = false;
                        currentT1plthr.PTCA = false;
                        currentT1plthr.PTPA = true;
                        new TXPLTHR().Save(currentT1plthr);
                    }
                    break;
                case PlaythroughStatus.Completed:
                    t1plthr.PTCO = true;
                    t1plthr.PTCA = false;
                    t1plthr.PTPA = false;
                    break;
                case PlaythroughStatus.Canceled:
                    t1plthr.PTCO = false;
                    t1plthr.PTCA = true;
                    t1plthr.PTPA = false;
                    break;
                case PlaythroughStatus.Paused:
                    t1plthr.PTCO = false;
                    t1plthr.PTCA = false;
                    t1plthr.PTPA = true;
                    break;
            }

            new TXPLTHR().Save(t1plthr);
        }

        public enum PlaythroughStatus
        {
            Open,
            Completed,
            Canceled,
            Paused
        }
    }
}
