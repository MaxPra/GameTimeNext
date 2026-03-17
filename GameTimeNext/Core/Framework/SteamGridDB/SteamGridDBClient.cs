using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.SteamGridDB
{
    public class SteamGridDBClient
    {
        private readonly HttpClient? _httpClient = null;
        private readonly Uri _steamGridDBUri = new Uri("https://www.steamgriddb.com/api/v2/");

        public SteamGridDBClient(string apiKey, HttpClient? httpClient = null)
        {
            if (FnString.IsNullEmptyOrWhitespace(apiKey))
                throw new ArgumentException("API-Key missing!", nameof(apiKey));

            _httpClient = httpClient ?? new HttpClient();
            _httpClient.BaseAddress = _steamGridDBUri;
            _httpClient.Timeout = TimeSpan.FromSeconds(20);

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                                                                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) GameTimeNext/1.0"
                                                                );
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd("*/*");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        public async Task<IReadOnlyList<SgdbGrid>> GetGridsBySteamAppIdAsync(int steamAppId,
                                                                             string? dimensions = "600x900",
                                                                             string? styles = null,
                                                                             CancellationToken cancellationToken = default)
        {
            List<string> query = new List<string>();

            if (!FnString.IsNullEmptyOrWhitespace(dimensions))
                query.Add($"dimensions={Uri.EscapeDataString(dimensions)}");

            if (!FnString.IsNullEmptyOrWhitespace(styles))
                query.Add($"styles={Uri.EscapeDataString(styles)}");

            string url = $"grids/steam/{steamAppId}";

            if (query.Count > 0)
                url += "?" + string.Join("&", query);

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            var result = await JsonSerializer.DeserializeAsync<SgdbResponse<List<SgdbGrid>>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);

            return result?.Data!;

        }

        public async Task<IReadOnlyList<SgdbGrid>> GetGridsByNameAsync(string name,
                                                               string? dimensions = "600x900",
                                                               string? styles = null,
                                                               CancellationToken cancellationToken = default)
        {
            if (FnString.IsNullEmptyOrWhitespace(name))
                return Array.Empty<SgdbGrid>();

            string searchUrl = $"search/autocomplete/{Uri.EscapeDataString(name)}";

            using var searchResponse = await _httpClient.GetAsync(searchUrl, cancellationToken);
            searchResponse.EnsureSuccessStatusCode();

            await using var searchStream = await searchResponse.Content.ReadAsStreamAsync(cancellationToken);

            using var document = await JsonDocument.ParseAsync(searchStream, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("data", out var data) || data.GetArrayLength() == 0)
                return Array.Empty<SgdbGrid>();

            int gameId = data[0].GetProperty("id").GetInt32();

            List<string> query = new List<string>();

            if (!FnString.IsNullEmptyOrWhitespace(dimensions))
                query.Add($"dimensions={Uri.EscapeDataString(dimensions)}");

            if (!FnString.IsNullEmptyOrWhitespace(styles))
                query.Add($"styles={Uri.EscapeDataString(styles)}");

            string url = $"grids/game/{gameId}";

            if (query.Count > 0)
                url += "?" + string.Join("&", query);

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            var result = await JsonSerializer.DeserializeAsync<SgdbResponse<List<SgdbGrid>>>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);

            return result?.Data ?? new List<SgdbGrid>();
        }


        public async Task DownloadFileAsync(string fileUrl, string targetPath, CancellationToken cancellationToken = default)
        {
            string? directory = Path.GetDirectoryName(targetPath);

            if (!FnString.IsNullEmptyOrWhitespace(directory))
                Directory.CreateDirectory(directory);

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(20);
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) GameTimeNext/1.0"
            );
            httpClient.DefaultRequestHeaders.Accept.ParseAdd("*/*");

            using var response = await httpClient.GetAsync(
                fileUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            );

            response.EnsureSuccessStatusCode();

            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);

            using Image image = await Image.LoadAsync(input, cancellationToken);

            await image.SaveAsJpegAsync(
                targetPath,
                new JpegEncoder
                {
                    Quality = 80
                },
                cancellationToken
            );
        }
    }

    public sealed class SgdbResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    public sealed class SgdbGrid
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("score")]
        public int Score { get; set; }

        [JsonPropertyName("style")]
        public string? Style { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("thumb")]
        public string? Thumb { get; set; }
    }
}
