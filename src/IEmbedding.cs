using AIOChatbot.Embeddings;

namespace AIOChatbot;

public interface IEmbedding
{
    Task<EmbeddingResult> GetEmbeddingsAsync(string[] textList, CancellationToken cancellationToken);
}
