using System.Diagnostics;
using System.Globalization;
using System.Net.Http;

namespace GameTimeNext.Core.Framework.Http
{
    /// <summary>
    /// HTTP handler between a HttpClient and the network that records every request of an external API in FnApiStatus
    /// (duration, status code, errors). The request itself is not changed; created with FnApiStatus.CreateHandler.
    /// </summary>
    public sealed class ApiStatusHandler : DelegatingHandler
    {
        private readonly ApiStatus _status;

        public ApiStatusHandler(ApiStatus status, HttpMessageHandler innerHandler) : base(innerHandler)
        {
            _status = status;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            long start = Stopwatch.GetTimestamp();

            try
            {
                HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                string? error = response.IsSuccessStatusCode
                    ? null
                    : string.Format(CultureInfo.InvariantCulture, "{0} {1}", (int)response.StatusCode, response.ReasonPhrase ?? response.StatusCode.ToString());

                _status.Record(Stopwatch.GetElapsedTime(start), response.StatusCode, error);

                return response;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Cancelled by the caller (e.g. a newer search): no failure of the API
                throw;
            }
            catch (Exception ex)
            {
                // Network errors, timeouts, DNS: the API couldn't be reached
                _status.Record(Stopwatch.GetElapsedTime(start), null, ex.Message);
                throw;
            }
        }
    }
}
