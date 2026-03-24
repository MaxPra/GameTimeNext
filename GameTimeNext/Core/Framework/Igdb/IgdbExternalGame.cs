using System.Text.Json.Serialization;

public class IgdbExternalGame
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("game")]
    public int Game { get; set; }

    [JsonPropertyName("uid")]
    public string Uid { get; set; }

    [JsonPropertyName("external_game_source")]
    public int ExternalGameSource { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}