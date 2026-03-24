using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GameTimeNext.Core.Framework.Igdb
{
    public static class FnTwitchAuthentication
    {

        public static async Task<string> GetAccessTokenAsync(
    string clientId,
    string clientSecret,
    CancellationToken cancellationToken = default)
        {
            try
            {
                Debug.WriteLine("1: GetAccessTokenAsync gestartet");

                if (string.IsNullOrWhiteSpace(clientId))
                    throw new InvalidOperationException("clientId is empty.");

                if (string.IsNullOrWhiteSpace(clientSecret))
                    throw new InvalidOperationException("clientSecret is empty.");

                clientId = clientId.Trim();
                clientSecret = clientSecret.Trim();

                using var handler = new HttpClientHandler
                {
                    UseProxy = false
                };

                using var httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(15)
                };

                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

                using var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["grant_type"] = "client_credentials"
                });

                using var response = await httpClient.PostAsync(
                    "https://id.twitch.tv/oauth2/token",
                    content,
                    cancellationToken).ConfigureAwait(false);

                var json = await response.Content.ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                var token = JsonSerializer.Deserialize<TwitchTokenResponse>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (token == null || string.IsNullOrWhiteSpace(token.AccessToken))
                    throw new InvalidOperationException("Access Token could not be read!");

                return token.AccessToken;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                throw;
            }
        }

        public static string GetAccessToken(
    string clientId,
    string clientSecret)
        {
            return GetAccessTokenAsync(clientId, clientSecret)
                .GetAwaiter()
                .GetResult();
        }

        public static async Task<string> GetExternalGameSourcesAsync(
    HttpClient httpClient,
    string clientId,
    string accessToken,
    CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/external_game_sources");
            request.Headers.Add("Client-ID", clientId);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(
                "fields id,name; limit 500;",
                Encoding.UTF8,
                "text/plain");

            using var response = await httpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public static string GetExternalGameSources(
            HttpClient httpClient,
            string clientId,
            string accessToken)
        {
            return GetExternalGameSourcesAsync(httpClient, clientId, accessToken)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
    }
}
