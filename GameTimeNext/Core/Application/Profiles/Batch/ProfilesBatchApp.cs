using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.General.BackgroundProcesses;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Application.TimeMonitoring;
using GameTimeNext.Core.Framework;
using GameTimeNext.Shared.Profiles;
using GameTimeNext.Shared.Profiles.Monitoring;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Threading;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Profiles.Batch
{
    public class ProfilesBatchApp : UIXApplication, IUIXBatchAppStarter
    {
        private TXPROFI _txprofi = new TXPROFI();

        public void Start(UIXApplication hostApplication)
        {
            HostApplication = hostApplication;
            CallDispatcher = hostApplication.CallDispatcher;
        }

        public override void InitializeApplicationOutput()
        {
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
        }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(ProfilesBatchApp));
        }

        #region Methods PUBLIC
        public List<ProfileDto> GetProfiles()
        {
            List<ProfileDto> filteredT1profis = new List<ProfileDto>();

            UIXQuery query = new UIXQuery(K1PROFI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1PROFI.Name, K1PROFI.Fields.PFID);

            query.AddWhere(K1PROFI.Name, K1PROFI.Fields.ARCH, QueryCompareType.NOT_EQUALS, true);

            query.AddOrderBy(K1PROFI.Name, K1PROFI.Fields.LAPL, OrderDirection.DESC);
            query.AddOrderBy(K1PROFI.Name, K1PROFI.Fields.PFID, OrderDirection.ASC);

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    long pfid = UIXQuery.GetInt64(reader, K1PROFI.Name, K1PROFI.Fields.PFID);
                    T1PROFI? t1profi = _txprofi.Read(pfid);
                    if (t1profi is null || !TFPROFI.IsExternalGame(t1profi)) continue;

                    filteredT1profis.Add(_txprofi.ToDto(t1profi));
                }
            }

            return filteredT1profis;
        }

        public ProfileDetailDto? GetProfile(long pfid)
        {
            T1PROFI? t1profi = _txprofi.Read(pfid);
            if (t1profi is null)
                return null;

            return _txprofi.ToDetailDto(t1profi);
        }

        /// <summary>
        /// Starts the monitoring for the phone. Nothing is changed if the UI thread gets to it only after
        /// SocketTimeouts.ChangeDeadline: the phone has given up waiting by then and would not know the game is monitored.
        /// </summary>
        /// <param name="receivedAt">Stopwatch timestamp at which the request was received</param>
        public StartMonitoringResponseMessage StartMonitoring(long pfid, bool createPlaythrough, long receivedAt)
        {
            return CallDispatcher.Invoke(() => ExecuteStartMonitoring(pfid, createPlaythrough, receivedAt));
        }

        /// <param name="receivedAt">Stopwatch timestamp at which the request was received</param>
        public StopMonitoringResponseMessage StopMonitoring(long receivedAt)
        {
            return CallDispatcher.Invoke(() => ExecuteStopMonitoring(receivedAt));
        }

        public MonitoringStateResponseMessage GetMonitoringState()
        {
            var state = CFGameTimeMonitoring.GetMonitoringState();

            if (!state.IsMonitoring)
                return new MonitoringStateResponseMessage(false, null, null);

            T1PROFI t1profi = _txprofi.Read(state.PFID)!;
            return new MonitoringStateResponseMessage(true, _txprofi.ToDto(t1profi), state.StartedAt);
        }
        #endregion

        #region Methods PRIVATE
        private StartMonitoringResponseMessage ExecuteStartMonitoring(long pfid, bool createPlaythrough, long receivedAt)
        {
            RequestTiming timing = new RequestTiming(receivedAt);
            StartMonitoringResponseMessage? response = null;

            try
            {
                response = RunStartMonitoring(pfid, createPlaythrough, receivedAt, timing);
                return response;
            }
            finally
            {
                string? report = CFRemoteMonitoring.IsExtendedLoggingEnabled() ? timing.GetSlowReport() : null;
                if (report is not null)
                    FnLog.AddWarning(this, string.Create(CultureInfo.InvariantCulture, $"Start of the monitoring for the phone took long ({response?.Status.ToString() ?? "failed"}): {report}"));
            }
        }

        private StartMonitoringResponseMessage RunStartMonitoring(long pfid, bool createPlaythrough, long receivedAt, RequestTiming timing)
        {
            if (RemoteMonitoringSocketServer.IsRequestExpired(this, receivedAt))
                return new StartMonitoringResponseMessage(pfid, MonitoringResponse.RequestExpired);

            if (!IsProfilesViewReady())
                return new StartMonitoringResponseMessage(pfid, MonitoringResponse.ProfilesAppNotReady);

            if (CFGameTimeMonitoring.IsMonitoring)
                return new StartMonitoringResponseMessage(pfid, MonitoringResponse.MonitoringAlreadyRunning);

            T1PROFI? t1profi = _txprofi.Read(pfid);
            if (t1profi is null)
                return new StartMonitoringResponseMessage(pfid, MonitoringResponse.ProfileNotFound);

            // The profile is being edited on the PC: it can't be monitored (the monitoring locks it); checked before a
            // playthrough is created
            if (CFGameTimeMonitoring.GetLockHolder(pfid) is not null)
                return new StartMonitoringResponseMessage(pfid, MonitoringResponse.ProfileLocked);

            timing.EndStep("checks");

            if (!TFPLTHR.HasCurrentPlaythrough(pfid))
            {
                if (!createPlaythrough)
                    return new StartMonitoringResponseMessage(pfid, MonitoringResponse.NoActivePlaythrough);

                long nextNumber = TFPLTHR.GetCurrentPlaythroughCount(pfid, PlaythroughType.NEW_PLAYTHROUGH) + 1;
                TFPLTHR.CreateNewPlaythrough(this, pfid, "Playthrough #" + nextNumber, PlaythroughType.NEW_PLAYTHROUGH);
                timing.EndStep("new playthrough");
            }

            CallDispatcher.Trigger("EXEV_SwitchToApplication", typeof(ProfilesApp).FullName!);
            timing.EndStep("switch application");

            if (!AppEnvironment.CurrentPfid.Equals(pfid))
            {
                AppEnvironment.CurrentPfid = pfid;
                CallDispatcher.Trigger("EXEV_SwitchProfile", pfid);
                timing.EndStep("switch profile");
            }

            // Switching tab and profile may have taken long (the library is built); checked again before the start
            if (RemoteMonitoringSocketServer.IsRequestExpired(this, receivedAt))
                return new StartMonitoringResponseMessage(pfid, MonitoringResponse.RequestExpired);

            // Locks the profile; it may have been opened for editing since the check above
            if (!CFGameTimeMonitoring.StartMonitoring(pfid, out _))
                return new StartMonitoringResponseMessage(pfid, MonitoringResponse.ProfileLocked);

            timing.EndStep("lock and start");

            CallDispatcher.Trigger("EXEV_GameTimeMonitoringStarted");
            timing.EndStep("started event");

            // After the answer: covering every monitor (transition windows) must not delay it past the phone's timeout.
            // Not if the monitoring was stopped meanwhile, the stop has ended no blackout then and it would stay on
            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                var state = CFGameTimeMonitoring.GetMonitoringState();
                if (!state.IsMonitoring || state.PFID != pfid)
                    return;

                long blackoutStartedAt = Stopwatch.GetTimestamp();
                CFRemoteMonitoring.BeginRemoteBlackout();

                // Runs after the answer, so it is not part of the request's report
                TimeSpan blackoutDuration = Stopwatch.GetElapsedTime(blackoutStartedAt);
                if (blackoutDuration >= RequestTiming.SlowThreshold && CFRemoteMonitoring.IsExtendedLoggingEnabled())
                    FnLog.AddWarning(this, string.Create(CultureInfo.InvariantCulture, $"Remote blackout took long to turn on: {blackoutDuration.TotalSeconds:0.00} s."));
            });

            return new StartMonitoringResponseMessage(pfid, MonitoringResponse.MonitoringStarted);
        }

        private StopMonitoringResponseMessage ExecuteStopMonitoring(long receivedAt)
        {
            RequestTiming timing = new RequestTiming(receivedAt);
            StopMonitoringResponseMessage? response = null;

            try
            {
                response = RunStopMonitoring(timing);
                return response;
            }
            finally
            {
                string? report = CFRemoteMonitoring.IsExtendedLoggingEnabled() ? timing.GetSlowReport() : null;
                if (report is not null)
                    FnLog.AddWarning(this, string.Create(CultureInfo.InvariantCulture, $"Stop of the monitoring for the phone took long ({response?.Status.ToString() ?? "failed"}): {report}"));
            }
        }

        private StopMonitoringResponseMessage RunStopMonitoring(RequestTiming timing)
        {
            if (!IsProfilesViewReady())
                return new StopMonitoringResponseMessage(MonitoringResponse.ProfilesAppNotReady);

            if (!CFGameTimeMonitoring.IsMonitoring)
                return new StopMonitoringResponseMessage(MonitoringResponse.MonitoringNotRunningYet);

            CFGameTimeMonitoring.StopMonitoring();
            timing.EndStep("stop");

            // Stores the session and ends the remote blackout
            CallDispatcher.Trigger("EXEV_GameTimeMonitoringStopped");
            timing.EndStep("stopped event");

            double minutes = CFGameTimeMonitoring.GetMonitoredTimeInMinutes();
            return new StopMonitoringResponseMessage(MonitoringResponse.MonitoringStopped, minutes, CFGameTimeMonitoring.IsSessionStored(minutes));
        }

        private bool IsProfilesViewReady()
        {
            string profilesApp = typeof(ProfilesApp).FullName!;

            return AppEnvironment.IsApplicationRunning(profilesApp)
                && AppEnvironment.IsApplicationInitialized(profilesApp)
                && CallDispatcher.IsRegistered("EXEV_SwitchToApplication")
                && CallDispatcher.IsRegistered("EXEV_SwitchProfile")
                && CallDispatcher.IsRegistered("EXEV_GameTimeMonitoringStarted")
                && CallDispatcher.IsRegistered("EXEV_GameTimeMonitoringStopped");
        }
        #endregion

        /// <summary>
        /// Durations of the steps of a request from the phone, so a slow start or stop on a user's PC can be traced in the
        /// log (only while CFRemoteMonitoring.IsExtendedLoggingEnabled). The first step is the wait until the UI thread got
        /// to the request.
        /// </summary>
        private sealed class RequestTiming
        {
            /// <summary>
            /// Total duration from which a request is reported.
            /// </summary>
            public static readonly TimeSpan SlowThreshold = TimeSpan.FromSeconds(2);

            private readonly long _receivedAt;
            private readonly List<(string Step, TimeSpan Duration)> _steps = new();
            private long _stepStartedAt;

            public RequestTiming(long receivedAt)
            {
                _receivedAt = receivedAt;
                _stepStartedAt = receivedAt;
                EndStep("waiting for the UI thread");
            }

            /// <summary>
            /// Ends the running step under this name; the next one starts now.
            /// </summary>
            public void EndStep(string step)
            {
                long now = Stopwatch.GetTimestamp();
                _steps.Add((step, Stopwatch.GetElapsedTime(_stepStartedAt, now)));
                _stepStartedAt = now;
            }

            /// <summary>
            /// Total duration and the durations of the steps, null if the request was faster than SlowThreshold.
            /// </summary>
            public string? GetSlowReport()
            {
                TimeSpan total = Stopwatch.GetElapsedTime(_receivedAt);
                if (total < SlowThreshold)
                    return null;

                string steps = string.Join(", ", _steps.Select(s => string.Create(CultureInfo.InvariantCulture, $"{s.Step} {s.Duration.TotalSeconds:0.00} s")));
                return string.Create(CultureInfo.InvariantCulture, $"{total.TotalSeconds:0.00} s in total; {steps}.");
            }
        }
    }
}
