namespace AIOChatbot;

public interface IEmbedding
{
    Task<List<float[]>> GetEmbeddingsAsync(string[] textList, CancellationToken cancellationToken);
}
