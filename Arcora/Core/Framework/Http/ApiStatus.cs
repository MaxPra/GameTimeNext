using System.Net;

namespace GameTimeNext.Core.Framework.Http
{
    /// <summary>
    /// Usage statistics of one external API since the app started (requests, errors, durations), recorded by
    /// ApiStatusHandler. Thread safe: requests run in any thread, the diagnostics read a snapshot.
    /// </summary>
    public sealed class ApiStatus
    {
        private readonly object _syncRoot = new object();

        private long _requestCount;
        private long _errorCount;
        private TimeSpan _totalDuration;
        private TimeSpan _lastDuration;
        private DateTime? _lastRequestAt;
        private DateTime? _lastSuccessAt;
        private DateTime? _lastErrorAt;
        private string _lastError = string.Empty;
        private HttpStatusCode? _lastStatusCode;
        private DateTime? _lastCheckAt;
        private TimeSpan _lastCheckDuration;
        private string _lastCheckError = string.Empty;

        public ApiStatus(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public long RequestCount { get { lock (_syncRoot) return _requestCount; } }

        public long ErrorCount { get { lock (_syncRoot) return _errorCount; } }

        public TimeSpan LastDuration { get { lock (_syncRoot) return _lastDuration; } }

        public TimeSpan AverageDuration
        {
            get
            {
                lock (_syncRoot)
                    return _requestCount == 0 ? TimeSpan.Zero : TimeSpan.FromTicks(_totalDuration.Ticks / _requestCount);
            }
        }

        public DateTime? LastRequestAt { get { lock (_syncRoot) return _lastRequestAt; } }

        public DateTime? LastSuccessAt { get { lock (_syncRoot) return _lastSuccessAt; } }

        public DateTime? LastErrorAt { get { lock (_syncRoot) return _lastErrorAt; } }

        /// <summary>
        /// Reason of the last failed request ("401 Unauthorized", exception message), empty if none failed yet.
        /// </summary>
        public string LastError { get { lock (_syncRoot) return _lastError; } }

        public HttpStatusCode? LastStatusCode { get { lock (_syncRoot) return _lastStatusCode; } }

        /// <summary>
        /// Time of the last active check (FnApiStatus.CheckAsync), null if never checked.
        /// </summary>
        public DateTime? LastCheckAt { get { lock (_syncRoot) return _lastCheckAt; } }

        public TimeSpan LastCheckDuration { get { lock (_syncRoot) return _lastCheckDuration; } }

        /// <summary>
        /// Reason why the last check failed; empty if it succeeded (or there was none).
        /// </summary>
        public string LastCheckError { get { lock (_syncRoot) return _lastCheckError; } }

        /// <summary>
        /// True if the most recent request failed.
        /// </summary>
        public bool IsFailing
        {
            get
            {
                lock (_syncRoot)
                    return _lastErrorAt != null && (_lastSuccessAt == null || _lastErrorAt > _lastSuccessAt);
            }
        }

        internal void RecordCheck(TimeSpan duration, string? error)
        {
            lock (_syncRoot)
            {
                _lastCheckAt = DateTime.Now;
                _lastCheckDuration = duration;
                _lastCheckError = error ?? string.Empty;
            }
        }

        internal void Record(TimeSpan duration, HttpStatusCode? statusCode, string? error)
        {
            lock (_syncRoot)
            {
                DateTime now = DateTime.Now;

                _requestCount++;
                _totalDuration += duration;
                _lastDuration = duration;
                _lastRequestAt = now;
                _lastStatusCode = statusCode;

                if (error == null)
                {
                    _lastSuccessAt = now;
                    return;
                }

                _errorCount++;
                _lastErrorAt = now;
                _lastError = error;
            }
        }
    }
}
