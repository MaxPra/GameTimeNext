using Arcora.Core.Application.DataManagers;
using Arcora.Core.Application.General;
using Arcora.Core.Application.General.BackgroundProcesses;
using Arcora.Core.Application.TableObjects;
using Arcora.Core.Application.TimeMonitoring;
using Arcora.Core.Framework;
using Arcora.Shared;
using Arcora.Shared.Profiles;
using Arcora.Shared.Profiles.Monitoring;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace Arcora.Core.Application.Profiles.Batch
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

            query.AddOrderBy(K1PROFI.Name, K1PROFI.Fields.FAVO, OrderDirection.DESC);
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
            RequestTiming timing = new RequestTiming(this, nameof(AvailableRequests.StartMonitoring), receivedAt);
            StartMonitoringResponseMessage? response = null;

            try
            {
                response = RunStartMonitoring(pfid, createPlaythrough, receivedAt, timing);
                return response;
            }
            finally
            {
                timing.LogAnswered(response?.Status.ToString() ?? "failed");

                string? report = CFRemoteMonitoring.IsExtendedLoggingEnabled() ? timing.GetSlowReport() : null;
                if (report is not null)
                    FnLog.AddWarning(this, string.Create(CultureInfo.InvariantCulture, $"Start of the monitoring for the phone took long ({response?.Status.ToString() ?? "failed"}): {report}"));
            }
        }

        private StartMonitoringResponseMessage RunStartMonitoring(long pfid, bool createPlaythrough, long receivedAt, RequestTiming timing)
        {
            timing.BeginStep("checks");

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

            if (!TFPLTHR.HasCurrentPlaythrough(pfid))
            {
                if (!createPlaythrough)
                    return new StartMonitoringResponseMessage(pfid, MonitoringResponse.NoActivePlaythrough);

                timing.BeginStep("new playthrough");
                long nextNumber = TFPLTHR.GetCurrentPlaythroughCount(pfid, PlaythroughType.NEW_PLAYTHROUGH) + 1;
                TFPLTHR.CreateNewPlaythrough(this, pfid, "Playthrough #" + nextNumber, PlaythroughType.NEW_PLAYTHROUGH);
            }

            timing.BeginStep("switch application");
            CallDispatcher.Trigger("EXEV_SwitchToApplication", typeof(ProfilesApp).FullName!);

            if (!AppEnvironment.CurrentPfid.Equals(pfid))
            {
                timing.BeginStep("switch profile");
                AppEnvironment.CurrentPfid = pfid;
                CallDispatcher.Trigger("EXEV_SwitchProfile", pfid);
            }

            timing.BeginStep("lock and start");

            // Switching tab and profile may have taken long (the library is built); checked again before the start
            if (RemoteMonitoringSocketServer.IsRequestExpired(this, receivedAt))
                return new StartMonitoringResponseMessage(pfid, MonitoringResponse.RequestExpired);

            // Locks the profile; it may have been opened for editing since the check above
            if (!CFGameTimeMonitoring.StartMonitoring(pfid, out _))
                return new StartMonitoringResponseMessage(pfid, MonitoringResponse.ProfileLocked);

            timing.BeginStep("started event");
            CallDispatcher.Trigger("EXEV_GameTimeMonitoringStarted");

            // After the answer: covering every monitor (transition windows) must not delay it past the phone's timeout.
            // Not if the monitoring was stopped meanwhile, the stop has ended no blackout then and it would stay on
            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                var state = CFGameTimeMonitoring.GetMonitoringState();
                if (!state.IsMonitoring || state.PFID != pfid)
                    return;

                RequestTiming.LogStepAndFlush(this, "Remote blackout: turning on...");

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
            RequestTiming timing = new RequestTiming(this, nameof(AvailableRequests.StopMonitoring), receivedAt);
            StopMonitoringResponseMessage? response = null;

            try
            {
                response = RunStopMonitoring(timing);
                return response;
            }
            finally
            {
                timing.LogAnswered(response?.Status.ToString() ?? "failed");

                string? report = CFRemoteMonitoring.IsExtendedLoggingEnabled() ? timing.GetSlowReport() : null;
                if (report is not null)
                    FnLog.AddWarning(this, string.Create(CultureInfo.InvariantCulture, $"Stop of the monitoring for the phone took long ({response?.Status.ToString() ?? "failed"}): {report}"));
            }
        }

        private StopMonitoringResponseMessage RunStopMonitoring(RequestTiming timing)
        {
            timing.BeginStep("stop");

            if (!IsProfilesViewReady())
                return new StopMonitoringResponseMessage(MonitoringResponse.ProfilesAppNotReady);

            if (!CFGameTimeMonitoring.IsMonitoring)
                return new StopMonitoringResponseMessage(MonitoringResponse.MonitoringNotRunningYet);

            CFGameTimeMonitoring.StopMonitoring();

            // Stores the session and ends the remote blackout
            timing.BeginStep("stopped event");
            CallDispatcher.Trigger("EXEV_GameTimeMonitoringStopped");

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
        /// Steps of a request from the phone, so a slow or hanging start or stop on a user's PC can be traced in the log
        /// (only while CFRemoteMonitoring.IsExtendedLoggingEnabled). Every step is logged when it begins and written through
        /// to the disk, so after a freeze of the PC the last entry names the step that hung; a slow request also gets a
        /// report of the step durations. The first step is the wait until the UI thread got to the request.
        /// </summary>
        private sealed class RequestTiming
        {
            /// <summary>
            /// Total duration from which a request is reported.
            /// </summary>
            public static readonly TimeSpan SlowThreshold = TimeSpan.FromSeconds(2);

            private readonly UIXApplication _app;
            private readonly string _request;
            private readonly long _receivedAt;
            private readonly bool _logSteps;
            private readonly List<(string Step, TimeSpan Duration)> _steps = new();
            private string _step = "waiting for the UI thread";
            private long _stepStartedAt;

            public RequestTiming(UIXApplication app, string request, long receivedAt)
            {
                _app = app;
                _request = request;
                _receivedAt = receivedAt;
                _stepStartedAt = receivedAt;
                _logSteps = CFRemoteMonitoring.IsExtendedLoggingEnabled();
            }

            /// <summary>
            /// Ends the running step and begins the next one under this name (logged at once).
            /// </summary>
            [StackTraceHidden]
            public void BeginStep(string step,
                [CallerMemberName] string callerMember = "", [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0)
            {
                long now = Stopwatch.GetTimestamp();
                _steps.Add((_step, Stopwatch.GetElapsedTime(_stepStartedAt, now)));
                _step = step;
                _stepStartedAt = now;

                if (_logSteps)
                    LogStepAndFlush(_app, $"{_request}: {step}...", callerMember, callerFile, callerLine);
            }

            /// <summary>
            /// Logs that the answer is on its way: a freeze after this entry comes from something run after the request
            /// (remote blackout, continuations of the triggered events).
            /// </summary>
            [StackTraceHidden]
            public void LogAnswered(string status,
                [CallerMemberName] string callerMember = "", [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0)
            {
                if (_logSteps)
                    LogStepAndFlush(_app, $"{_request}: answered ({status}).", callerMember, callerFile, callerLine);
            }

            /// <summary>
            /// Logs the message and writes the log through to the disk, so the entry survives a freeze right after.
            /// </summary>
            [StackTraceHidden]
            public static void LogStepAndFlush(UIXApplication app, string message,
                [CallerMemberName] string callerMember = "", [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0)
            {
                FnLog.AddInfo(app, message, null, callerMember, callerFile, callerLine);
                AppEnvironment.GetLogDataBaseManager().FlushToDisk();
            }

            /// <summary>
            /// Total duration and the durations of the steps, null if the request was faster than SlowThreshold.
            /// </summary>
            public string? GetSlowReport()
            {
                long now = Stopwatch.GetTimestamp();
                TimeSpan total = Stopwatch.GetElapsedTime(_receivedAt, now);
                if (total < SlowThreshold)
                    return null;

                IEnumerable<(string Step, TimeSpan Duration)> steps = _steps.Append((_step, Stopwatch.GetElapsedTime(_stepStartedAt, now)));
                string text = string.Join(", ", steps.Select(s => string.Create(CultureInfo.InvariantCulture, $"{s.Step} {s.Duration.TotalSeconds:0.00} s")));
                return string.Create(CultureInfo.InvariantCulture, $"{total.TotalSeconds:0.00} s in total; {text}.");
            }
        }
    }
}
