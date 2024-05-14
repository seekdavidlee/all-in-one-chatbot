namespace AIOChatbot.Embeddings;

public class EmbeddingResult
{
    public EmbeddingResult()
    {
        Vectors = [];
    }
    public int TotalTokens { get; set; }

    public double? DurationInMilliseconds { get; set; }

    public List<float[]> Vectors { get; set; }
}
