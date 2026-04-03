using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;

namespace GameTimeNext.Core.Application.TimeMonitoring
{
    public class CFGameTimeMonitoring
    {
        public static Boolean IsMonitoring { get; set; } = false;

        private static DateTime _startTime;
        private static DateTime _endTime;
        private static long _monitoringPFID = 0;
        private static double _monitoredTime = 0;

        /// <summary>
        /// Startet das Aufzeichnen der Spielzeit
        /// </summary>
        /// <param name="pfid">Profil ID</param>
        public static void StartMonitoring(long pfid)
        {
            if (pfid == 0)
                return;

            IsMonitoring = true;
            _monitoringPFID = pfid;
            _startTime = DateTime.Now;
        }

        /// <summary>
        /// Stoppt das Aufzeichnen der Spielzeit
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

        public static void UpdateTableObject()
        {
            // -- Sessiondaten befüllen
            TXSESSI txsessi = new TXSESSI();
            T1SESSI t1sessi = txsessi.CreateNew();

            t1sessi.PFID = AppEnvironment.CurrentPfid;
            t1sessi.PLFR = _startTime;
            t1sessi.PLTO = _endTime;
            t1sessi.PLTI = GetMonitoredTimeInMinutes();

            // Derzeitigen Playthrough ermitteln
            long ptid = TFPLTHR.GetCurrentPlaythroughPtid(t1sessi.PFID);

            t1sessi.PTID = ptid;

            txsessi.Save(t1sessi);

            // -- Profildaten befüllen
            TXPROFI txprofi = new TXPROFI();
            T1PROFI t1profi = txprofi.Read(AppEnvironment.CurrentPfid);

            if (t1profi.FIPL == DateTime.MinValue)
                t1profi.FIPL = _startTime;

            t1profi.LAPL = _endTime;

            txprofi.Save(t1profi);
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
    }
}
