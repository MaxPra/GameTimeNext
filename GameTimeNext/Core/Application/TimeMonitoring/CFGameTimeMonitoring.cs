using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles.BackgroundProcesses;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Utils;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.TimeMonitoring
{
    public class CFGameTimeMonitoring
    {
        public static Boolean IsMonitoring { get; set; } = false;

        private static DateTime _startTime;
        private static DateTime _endTime;
        private static long _monitoringPFID = 0;
        private static double _monitoredTime = 0;

        // Records of the monitored game (profile, playthroughs, group positions), locked by the game detection while it is
        // monitored, so the profile can't be edited meanwhile. Start and stop come from the UI thread and the hotkey thread.
        private static readonly object SyncRoot = new object();
        private static readonly List<UIXTableObjectBase> _lockedRecords = new List<UIXTableObjectBase>();

        /// <summary>
        /// Holder of the record locks of the monitored game: the game detection (GameRunningProcess), which runs as long as
        /// the app, so no closed view or tab releases them.
        /// </summary>
        public static UIXApplication GetLockOwner()
        {
            return AppEnvironment.StartedBackgroundProcesses[typeof(GameRunningProcess).FullName!];
        }

        /// <summary>
        /// True if the monitoring holds the lock of the record (it belongs to the monitored game).
        /// </summary>
        public static bool HoldsLock(UIXTableObjectBase record)
        {
            return record.GetLockHolder()?.Holder is GameRunningProcess;
        }

        /// <summary>
        /// Checks before the monitoring is started (and before asking for a new playthrough) that nobody edits the profile
        /// or its linked records. Otherwise tells the user where it is open (GLO0023, on the UI thread) and returns false.
        /// </summary>
        public static bool CheckNotLocked(UIXApplication app, long pfid)
        {
            UIXTableObjectBase? locked = TFPROFI.GetProfileWithLinkedRecords(GetLockOwner(), pfid).FirstOrDefault(r => r.IsLocked());

            if (locked?.GetLockHolder() is not UIXRecordLockInfo lockedBy)
                return true;

            FnRecordLock.ShowLocked(app, lockedBy);
            return false;
        }

        /// <summary>
        /// Starts recording the playtime and locks the profile, its playthroughs and group positions for the game detection.
        /// If one of them is being edited, nothing is started, the user is told where it is open (GLO0023, on the UI thread)
        /// and false is returned.
        /// </summary>
        /// <param name="app">Application that starts the monitoring (shows the message)</param>
        /// <param name="pfid">Profile ID</param>
        public static bool StartMonitoring(UIXApplication app, long pfid)
        {
            // Nothing to monitor; the callers continue as before
            if (pfid == 0)
                return true;

            if (!LockRecords(pfid, out UIXRecordLockInfo? lockedBy))
            {
                FnRecordLock.ShowLocked(app, lockedBy);
                return false;
            }

            IsMonitoring = true;
            _monitoringPFID = pfid;
            _startTime = DateTime.Now;

            return true;
        }

        /// <summary>
        /// Stops recording the playtime. The records stay locked until the session is stored (UpdateTableObject).
        /// </summary>
        public static void StopMonitoring()
        {
            IsMonitoring = false;

            _endTime = DateTime.Now;

            TimeSpan monitorTimeSpan = _endTime - _startTime;

            _monitoredTime = monitorTimeSpan.TotalSeconds;
        }

        public static double GetMonitoredTimeInMinutes()
        {
            if (_monitoredTime == 0)
                return 0;

            return ((double)_monitoredTime / 60);
        }

        /// <summary>
        /// Stores the session of the stopped monitoring and the play dates of the profile, then releases the records.
        /// Stored for the monitored profile, which holds the locks: the current profile may have changed meanwhile (another
        /// game started, the blackout of the Big Picture mode selects the shown game).
        /// </summary>
        public static void UpdateTableObject()
        {
            try
            {
                if (_monitoringPFID == 0)
                    return;

                UIXApplication owner = GetLockOwner();

                // -- Fill session data
                TXSESSI txsessi = owner.GetDataManager<TXSESSI>();
                T1SESSI t1sessi = txsessi.CreateNew();

                t1sessi.PFID = _monitoringPFID;
                t1sessi.PLFR = _startTime;
                t1sessi.PLTO = _endTime;
                t1sessi.PLTI = GetMonitoredTimeInMinutes();
                if (AppEnvironment.GetAppConfig().AppSettings.EnableSessionCleanup && t1sessi.PLTI < (AppEnvironment.GetAppConfig().AppSettings.SessionCleanupSeconds / 60)) return;

                // Determine the current playthrough
                long ptid = TFPLTHR.GetCurrentPlaythroughPtid(t1sessi.PFID);

                t1sessi.PTID = ptid;

                txsessi.Save(t1sessi);

                // -- Fill profile data
                TXPROFI txprofi = owner.GetDataManager<TXPROFI>();
                T1PROFI t1profi = txprofi.Read(_monitoringPFID);

                if (t1profi.FIPL == DateTime.MinValue)
                    t1profi.FIPL = _startTime;

                t1profi.LAPL = _endTime;

                txprofi.Save(t1profi);
            }
            finally
            {
                ReleaseLocks();
            }
        }

        /// <summary>
        /// Gets the elapsed game time, in minutes, since monitoring started.
        /// </summary>
        /// <returns>The number of minutes that have elapsed since monitoring began. Returns 0.0 if monitoring is not active or
        /// the monitoring session is not initialized.</returns>
        public static double GetCurrentGameTimeMinutes()
        {
            if (!IsMonitoring)
                return 0.0;

            if (_monitoringPFID == 0)
                return 0.0;

            TimeSpan currentTimeSpan = DateTime.Now - _startTime;

            return (double)currentTimeSpan.TotalSeconds / 60;
        }

        private static bool LockRecords(long pfid, out UIXRecordLockInfo? lockedBy)
        {
            lockedBy = null;

            lock (SyncRoot)
            {
                // Left over from a monitoring whose session wasn't stored
                ReleaseLocksCore();

                foreach (UIXTableObjectBase record in TFPROFI.GetProfileWithLinkedRecords(GetLockOwner(), pfid))
                {
                    if (!record.Lock())
                    {
                        lockedBy = record.GetLockHolder();
                        ReleaseLocksCore();
                        return false;
                    }

                    _lockedRecords.Add(record);
                }
            }

            return true;
        }

        private static void ReleaseLocks()
        {
            lock (SyncRoot)
            {
                ReleaseLocksCore();
            }
        }

        private static void ReleaseLocksCore()
        {
            foreach (UIXTableObjectBase record in _lockedRecords)
                record.Unlock();

            _lockedRecords.Clear();
        }
    }
}
