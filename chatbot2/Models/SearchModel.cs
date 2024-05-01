using System.Text.Json.Serialization;

namespace chatbot2;

public class SearchModel
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("filepath")]
    public string? Filepath { get; set; }

    [JsonPropertyName("meta_json_string")]
    public string? MetaData { get; set; }

    [JsonPropertyName("contentVector")]
    public float[]? ContentVector { get; set; }

    public string? ContentToVectorized { get; set; }
}
