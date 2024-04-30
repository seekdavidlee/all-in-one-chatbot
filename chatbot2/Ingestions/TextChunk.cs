using SharpToken;

namespace chatbot2.Ingestions;

public class TextChunk
{
    private static readonly GptEncoding gptEncoding = GptEncoding.GetEncoding(Model.GetEncodingNameForModel(Environment.GetEnvironmentVariable("TextEmbeddingName")));
    public TextChunk(string id, string text)
    {
        Id = id;
        Text = text;
        TokenCount = gptEncoding.CountTokens(text);
    }

    public string Id { get; }
    public string? Text { get; set; }
    public Dictionary<string, object> MetaDatas { get; set; } = [];

    public int TokenCount { get; }
}


