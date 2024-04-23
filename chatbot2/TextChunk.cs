namespace chatbot2;

public class TextChunk
{
    public TextChunk(string id, string text)
    {
        Id = id;
        Text = text;
    }

    public string Id { get; }
    public string? Text { get; set; }
    public Dictionary<string, object> MetaDatas { get; set; } = [];
}


