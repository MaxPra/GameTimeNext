using RemoteMonitoring.ApiServices;
using System.Security.Cryptography;
using System.Text;

namespace RemoteMonitoring.Base
{
    /// <summary>
    /// Pairing code of arcora (asked over the socket, kept while arcora can't be reached). A phone is paired when its
    /// cookie holds the current code; "Reset pairing" in arcora changes the code and so unpairs every phone.
    /// </summary>
    public class PairingService
    {
        public const string CookieName = "rm_pairing";

        // Age after which the code is asked from arcora again before a check (a reset takes effect within this time)
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(5);

        private readonly SocketClient _socket;
        private readonly SystemApiService _systemApiService;
        private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);

        private string? _code;
        private DateTime _refreshedAt = DateTime.MinValue;

        public PairingService(SocketClient socket, SystemApiService systemApiService)
        {
            _socket = socket;
            _systemApiService = systemApiService;

            // A new connection may mean a new arcora session or a reset code
            _socket.ConnectionStateChanged += (_, state) =>
            {
                if (state == ConnectionState.Connected)
                    _ = RefreshAsync(force: true);
            };
        }

        /// <summary>
        /// Checks the code against the current pairing code of arcora (constant time comparison). Unreachable while the
        /// current code is unknown, so a valid cookie isn't taken for an unpaired phone.
        /// </summary>
        public async Task<PairingCheckResult> CheckAsync(string? code)
        {
            if (string.IsNullOrEmpty(code))
                return PairingCheckResult.NotPaired;

            // The known code may be outdated ("Reset pairing"): compared only with one at most a few seconds old,
            // otherwise an old cookie would keep matching the old code forever
            await RefreshAsync(force: false);

            string? current = _code;
            if (current is null)
                return PairingCheckResult.Unreachable;

            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(code), Encoding.UTF8.GetBytes(current))
                ? PairingCheckResult.Paired
                : PairingCheckResult.NotPaired;
        }

        private async Task RefreshAsync(bool force)
        {
            if (_socket.State != ConnectionState.Connected)
                return;

            await _refreshLock.WaitAsync();
            try
            {
                if (!force && DateTime.Now - _refreshedAt < RefreshInterval)
                    return;

                string? code = await _systemApiService.GetPairingCode();
                if (code is not null)
                    _code = code;

                _refreshedAt = DateTime.Now;
            }
            finally
            {
                _refreshLock.Release();
            }
        }
    }
}
