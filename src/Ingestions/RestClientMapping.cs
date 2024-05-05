namespace AIOChatbot.Ingestions;

public class RestClientMapping
{
    public string[]? Sources { get; set; }
    public string? Target { get; set; }
    public string? SourcesJoinChar { get; set; }
    public string? Conversion { get; set; }

    public IDictionary<string, string>? Tags { get; set; }
}
