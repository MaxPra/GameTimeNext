using GameTimeNext.Core.Framework.UI.Dialogs;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.Igdb
{
    public class IgdbService
    {
        private UIXApplication _application;
        public HttpClient? HttpClient { get; set; } = null;
        public string ClientId { get; set; } = string.Empty;

        public static string? AuthToken { get; set; } = null;
        public static string IgdbExtGameSources { get; set; } = string.Empty;

        public IgdbService(UIXApplication app, string clientId)
        {
            _application = app;
            HttpClient = new HttpClient();
            ClientId = clientId;
        }

        public async Task<int?> FindGameIdBySteamAppIdAsync(
            int steamSourceId,
            string steamAppId,
            CancellationToken cancellationToken = default)
        {
            EnsureAuthToken();

            var body =
                "fields id,game,uid,external_game_source,url;" +
                $" where uid = \"{steamAppId}\" & external_game_source = {steamSourceId};" +
                " limit 1;";

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/external_games");
            request.Headers.Add("Client-ID", ClientId);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(body, Encoding.UTF8, "text/plain");

            using var response = await HttpClient!.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            var items = JsonSerializer.Deserialize<List<IgdbExternalGame>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return items?.FirstOrDefault()?.Game;
        }

        public async Task<IgdbGame?> FindGameByNameAsync(
            string gameName,
            CancellationToken cancellationToken = default)
        {
            EnsureAuthToken();

            var body =
                "fields id,name,slug,first_release_date;" +
                $" search \"{gameName.Replace("\"", "\\\"")}\";" +
                " limit 5;";

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/games");
            request.Headers.Add("Client-ID", ClientId);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(body, Encoding.UTF8, "text/plain");

            using var response = await HttpClient!.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            var items = JsonSerializer.Deserialize<List<IgdbGame>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return items?.FirstOrDefault();
        }

        public async Task<IgdbGameTimeToBeat?> GetGameTimeToBeatAsync(
            int gameId,
            CancellationToken cancellationToken = default)
        {
            EnsureAuthToken();

            var body =
                "fields id,game_id,hastily,normally,completely,count;" +
                $" where game_id = {gameId};" +
                " limit 1;";

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/game_time_to_beats");
            request.Headers.Add("Client-ID", ClientId);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(body, Encoding.UTF8, "text/plain");

            using var response = await HttpClient!.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            var items = JsonSerializer.Deserialize<List<IgdbGameTimeToBeat>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return items?.FirstOrDefault();
        }

        public int GetSteamSourceId()
        {
            EnsureAuthToken();

            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/external_game_sources");
            req.Headers.Add("Client-ID", ClientId);
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthToken);
            req.Content = new StringContent("fields id,name; limit 200;", Encoding.UTF8, "text/plain");

            var res = HttpClient!.SendAsync(req).GetAwaiter().GetResult();
            res.EnsureSuccessStatusCode();

            var json = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            var doc = JsonDocument.Parse(json);

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var name = item.GetProperty("name").GetString();

                if (name != null && name.Contains("Steam", StringComparison.OrdinalIgnoreCase))
                {
                    return item.GetProperty("id").GetInt32();
                }
            }

            throw new Exception("Steam Source nicht gefunden");
        }

        private bool EnsureAuthToken()
        {
            if (AuthToken is null)
                InitializeIGDBAuthTokenAndExternalGameSources();

            return AuthToken is not null;
        }

        private void InitializeIGDBAuthTokenAndExternalGameSources()
        {
            CFMBOX cfmbox = _application.GetApplication<CFMBOX>();
            bool enabled = AppEnvironment.GetAppConfig().AppSettings.EnableTwitchIGDB;
            string clientId = AppEnvironment.GetAppConfig().AppSettings.TwitchIGDBClientID;
            string clientSecret = AppEnvironment.GetAppConfig().AppSettings.TwitchIGDBClientSecret;

            if (!enabled || FnString.IsNullEmptyOrWhitespace(clientId) || FnString.IsNullEmptyOrWhitespace(clientSecret))
                return;

            try
            {
                AuthToken = FnTwitchAuthentication.GetAccessToken(clientId, clientSecret);
            }
            catch (Exception)
            {
                AuthToken = string.Empty;
            }

            if (FnString.IsNullEmptyOrWhitespace(AuthToken))
            {
                cfmbox.Show("Couldn't get auth-token for IGDB!", CFMBOXResult.Ok, CFMBOXIcon.Error);
                return;
            }

            try
            {
                IgdbExtGameSources = FnTwitchAuthentication.GetExternalGameSources(new System.Net.Http.HttpClient(), clientId, AuthToken);
            }
            catch (Exception)
            {
                IgdbExtGameSources = string.Empty;
            }


            if (FnString.IsNullEmptyOrWhitespace(IgdbExtGameSources))
                cfmbox.Show("Couldn't get external game sources from IGDB!", CFMBOXResult.Ok, CFMBOXIcon.Error);
        }
    }
}
