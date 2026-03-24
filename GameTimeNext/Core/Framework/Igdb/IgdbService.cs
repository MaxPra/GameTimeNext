using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GameTimeNext.Core.Framework.Igdb
{
    public class IgdbService
    {

        public HttpClient? HttpClient { get; set; } = null;
        public string ClientId { get; set; } = string.Empty;

        public string AccessToken { get; set; }

        public IgdbService(string clientId, string accessToken)
        {
            HttpClient = new HttpClient();
            ClientId = clientId;
            AccessToken = accessToken;
        }

        public async Task<int?> FindGameIdBySteamAppIdAsync(
            int steamSourceId,
            string steamAppId,
            CancellationToken cancellationToken = default)
        {
            var body =
                "fields id,game,uid,external_game_source,url;" +
                $" where uid = \"{steamAppId}\" & external_game_source = {steamSourceId};" +
                " limit 1;";

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/external_games");
            request.Headers.Add("Client-ID", ClientId);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
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
            var body =
                "fields id,name,slug,first_release_date;" +
                $" search \"{gameName.Replace("\"", "\\\"")}\";" +
                " limit 5;";

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/games");
            request.Headers.Add("Client-ID", ClientId);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
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
            var body =
                "fields id,game_id,hastily,normally,completely,count;" +
                $" where game_id = {gameId};" +
                " limit 1;";

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/game_time_to_beats");
            request.Headers.Add("Client-ID", ClientId);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
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
            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/external_game_sources");
            req.Headers.Add("Client-ID", ClientId);
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);
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
    }
}
