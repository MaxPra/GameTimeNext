using Arcora.Core.Application.Playthroughs;
using Arcora.Core.Application.TableObjects;
using Arcora.Core.Framework;
using Arcora.Core.Framework.UI.Dialogs;
using Arcora.Core.Framework.Utils;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace Arcora.Core.Application.DataManagers
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

        public static T1PLTHR GetCurrentPlaythrough(UIXApplication app, long pfid)
        {
            return app.GetDataManager<TXPLTHR>().Read(GetCurrentPlaythroughPtid(pfid));
        }

        public static bool HasCurrentPlaythrough(long pfid)
        {
            // Only read for the answer, never saved
            T1PLTHR currT1plthr = new TXPLTHR().Read(GetCurrentPlaythroughPtid(pfid));

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
        public static long CreateNewPlaythrough(UIXApplication app, long pfid, string description = "Playthrough #1 (Initial)", string type = PlaythroughType.INITIAL_PLAYTHROUGH)
        {
            TXPLTHR txplthr = app.GetDataManager<TXPLTHR>();
            T1PLTHR t1plthr = txplthr.CreateNew();

            t1plthr.PFID = pfid;
            t1plthr.PTDE = description;
            t1plthr.PTTY = type;

            txplthr.Save(t1plthr);

            return t1plthr.PTID;

        }

        public static List<T1SESSI> GetSessions(UIXApplication app, long ptid)
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

                    TXSESSI txsessi = app.GetDataManager<TXSESSI>();
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

        public static void DeletePlaythroughAndSessions(UIXApplication app, long ptid)
        {
            TXPLTHR txplthr = app.GetDataManager<TXPLTHR>();
            TXSESSI txsessi = app.GetDataManager<TXSESSI>();

            List<T1SESSI> sessions = GetSessions(app, ptid);

            foreach (T1SESSI session in sessions)
            {
                txsessi.Delete(session.SEID);
            }

            txplthr.Delete(ptid);
        }

        /// <summary>
        /// Changes the status of the playthrough (and pauses the current one when another is opened again), after asking.
        /// The playthroughs are locked meanwhile (record lock) and read again, so changes made elsewhere since the list
        /// was loaded (e.g. a rating) aren't overwritten; t1plthr is the stored playthrough afterwards. If one of them is
        /// being edited or its game is monitored, nothing is asked or changed and the user is told why.
        /// </summary>
        public static void UpdatePlaythroughStatus(UIXApplication app, ref T1PLTHR t1plthr, PlaythroughStatus status, T1PLTHR? currentT1plthr = null)
        {
            T1PLTHR target = t1plthr;
            bool rate = false;

            List<UIXTableObjectBase> records = new List<UIXTableObjectBase> { target };
            if (currentT1plthr is not null)
                records.Add(currentT1plthr);

            FnRecordLock.Run(app, () =>
            {
                TXPLTHR txplthr = app.GetDataManager<TXPLTHR>();
                T1PLTHR? current = currentT1plthr is not null ? txplthr.Read(currentT1plthr.PTID) ?? currentT1plthr : null;

                target = txplthr.Read(target.PTID) ?? target;

                if (ChangeStatus(app, txplthr, target, status, current))
                    rate = status == PlaythroughStatus.Completed || status == PlaythroughStatus.Canceled;
            }, records.ToArray());

            t1plthr = target;

            // Optional rating after a playthrough has ended, can simply be skipped (its view locks the playthrough itself)
            if (rate)
                app.GetApplication<PlaythroughRatingApp>().Rate(null, t1plthr);
        }

        // Asks and changes the status; false if the user said no
        private static bool ChangeStatus(UIXApplication app, TXPLTHR txplthr, T1PLTHR t1plthr, PlaythroughStatus status, T1PLTHR? currentT1plthr)
        {
            string question = string.Empty;

            // Completing or canceling can't be undone: stated like a deletion, the button has to be held
            string irreversibleMessage = string.Empty;
            string irreversibleAction = string.Empty;

            switch (status)
            {
                case PlaythroughStatus.Open:
                    if (!t1plthr.PTPA) break;

                    question = app.GetErrorMessage("PLT0002", "unpause");
                    if (currentT1plthr is not null)
                        question += "\n\n" + app.GetErrorMessage("PLT0004");
                    break;
                case PlaythroughStatus.Completed:
                    irreversibleMessage = app.GetErrorMessage("PLT0003", "completed");
                    irreversibleAction = "Complete";
                    break;
                case PlaythroughStatus.Canceled:
                    irreversibleMessage = app.GetErrorMessage("PLT0003", "canceled") + "\n" + app.GetErrorMessage("PLT0005");
                    irreversibleAction = "Cancel Playthrough";
                    break;
                case PlaythroughStatus.Paused:
                    question = app.GetErrorMessage("PLT0002", "pause");
                    break;
            }

            if (!FnString.IsNullEmptyOrWhitespace(question))
            {
                CFMBOXResult result = app.GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(question, CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);
                if (result == CFMBOXResult.No) return false;
            }

            if (!FnString.IsNullEmptyOrWhitespace(irreversibleMessage) &&
                !app.GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).ConfirmDestructive(irreversibleMessage, irreversibleAction))
                return false;

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
                        txplthr.Save(currentT1plthr);
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

            txplthr.Save(t1plthr);

            return true;
        }

        /// <summary>
        /// Rating of a profile: average of all rated playthroughs (PTRA scale, 1 to 10), 0 if none is rated.
        /// Completed and canceled playthroughs count the same.
        /// </summary>
        public static double GetProfileRating(long pfid)
        {
            UIXQuery query = new UIXQuery(K1PLTHR.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddAggregate(UIXQuery.AggregateFunc.AVG, K1PLTHR.Name, K1PLTHR.Fields.PTRA, alias: "K1PLTHR_AVG_PTRA");

            query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PFID, QueryCompareType.EQUALS, pfid);
            query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PTRA, QueryCompareType.GREATER_THAN, 0);

            using (var reader = query.Execute())
                if (reader.Read())
                    return UIXQuery.GetDouble(reader, "K1PLTHR_AVG_PTRA");

            return 0;
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
