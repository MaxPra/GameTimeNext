using Arcora.Shared.Profiles.Monitoring;
using RemoteMonitoring.ApiServices;

namespace RemoteMonitoring.Base
{
    /// <summary>
    /// Asks arcora every few seconds which game is monitored and tells the subscribed pages when that changes. One
    /// query for all open pages; a page refreshes it itself right after a start or stop.
    /// </summary>
    public class MonitoringStateService : BackgroundService
    {
        private readonly SocketClient _socket;
        private readonly ProfilesApiService _profilesApiService;

        // Timer and pages (after Start/Stop) may refresh at the same time: one at a time, so an older answer
        // never overwrites a newer one
        private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);

        public MonitoringStateResponseMessage? Current { get; private set; }

        public event EventHandler<MonitoringStateResponseMessage?>? Changed;

        public MonitoringStateService(SocketClient socket, ProfilesApiService profilesApiService)
        {
            _socket = socket;
            _profilesApiService = profilesApiService;

            // Ask at once after a reconnect instead of waiting for the next tick (redirect, timer)
            _socket.ConnectionStateChanged += OnConnectionStateChanged;
        }

        private void OnConnectionStateChanged(object? sender, ConnectionState state)
        {
            if (state == ConnectionState.Connected)
                _ = RefreshAsync();
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
            do { await RefreshAsync(); }
            while (await timer.WaitForNextTickAsync(ct));
        }

        /// <summary>
        /// Asks arcora for the current monitoring and tells the subscribers if it changed. Keeps the last state while
        /// there is no connection or no answer.
        /// </summary>
        public async Task RefreshAsync()
        {
            // Without a connection the request would fail and log an error every few seconds
            if (_socket.State != ConnectionState.Connected)
                return;

            await _refreshLock.WaitAsync();
            try
            {
                MonitoringStateResponseMessage? state = await _profilesApiService.GetMonitoringState();

                // No answer (timeout, reconnect): keep the last known state, the next tick tries again
                if (state is null || IsSame(Current, state))
                    return;

                Current = state;
                Changed?.Invoke(this, Current);
            }
            catch (Exception ex)
            {
                // An exception leaving ExecuteAsync would stop the whole web app (BackgroundService default)
                FnLog.AddError(this, "Refreshing the monitoring state failed.", ex);
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        /// <summary>
        /// Same monitoring: same profile and same start (a stop and restart of the same game is a new one).
        /// </summary>
        private static bool IsSame(MonitoringStateResponseMessage? current, MonitoringStateResponseMessage state)
        {
            return current is not null
                && current.IsMonitoring == state.IsMonitoring
                && current.Profile?.PFID == state.Profile?.PFID
                && current.StartedAt == state.StartedAt;
        }
    }
}
