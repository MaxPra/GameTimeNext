using System.Text.Json.Serialization;

public class IgdbGame
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("first_release_date")]
    public int? FirstReleaseDate { get; set; }
}