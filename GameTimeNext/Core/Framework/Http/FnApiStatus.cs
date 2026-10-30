using System.Diagnostics;
using System.Net.Http;

namespace GameTimeNext.Core.Framework.Http
{
    /// <summary>
    /// Status of the external APIs (SteamGridDB, IGDB) since the app started, shown in the module "Diagnostics".
    /// Every HttpClient of an API gets its handler from CreateHandler, so all its requests are counted.
    /// </summary>
    public static class FnApiStatus
    {
        public const string SteamGridDB = "SteamGridDB";

        /// <summary>
        /// IGDB including the Twitch authentication it needs.
        /// </summary>
        public const string Igdb = "IGDB";

        // Fixed list: every API is shown, also before its first request
        private static readonly IReadOnlyList<ApiStatus> Statuses = new List<ApiStatus>
        {
            new ApiStatus(SteamGridDB),
            new ApiStatus(Igdb)
        };

        /// <summary>
        /// Handler for a HttpClient of the given API (SteamGridDB, Igdb) that counts its requests; innerHandler is the
        /// handler that sends them (default: a new HttpClientHandler).
        /// </summary>
        public static HttpMessageHandler CreateHandler(string apiName, HttpMessageHandler? innerHandler = null)
        {
            return new ApiStatusHandler(Get(apiName), innerHandler ?? new HttpClientHandler());
        }

        public static ApiStatus Get(string apiName)
        {
            return Statuses.FirstOrDefault(s => s.Name == apiName)
                ?? throw new ArgumentException($"Unknown API \"{apiName}\".", nameof(apiName));
        }

        public static IReadOnlyList<ApiStatus> GetAll()
        {
            return Statuses;
        }

        /// <summary>
        /// Checks the API now with the given test request and stores the result in the status (LastCheckAt, LastCheckError).
        /// Never throws for a failed check; a timeout counts as failure, a cancellation by the caller is passed on.
        /// </summary>
        public static async Task CheckAsync(ApiStatus status, Func<CancellationToken, Task> check, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);

            long start = Stopwatch.GetTimestamp();

            try
            {
                await check(timeoutSource.Token).ConfigureAwait(false);
                status.RecordCheck(Stopwatch.GetElapsedTime(start), null);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                status.RecordCheck(Stopwatch.GetElapsedTime(start), $"No answer within {timeout.TotalSeconds:0} s");
            }
            catch (HttpRequestException ex) when (ex.StatusCode != null)
            {
                status.RecordCheck(Stopwatch.GetElapsedTime(start), $"{(int)ex.StatusCode.Value} {ex.StatusCode.Value}");
            }
            catch (Exception ex)
            {
                status.RecordCheck(Stopwatch.GetElapsedTime(start), ex.Message);
            }
        }
    }
}
