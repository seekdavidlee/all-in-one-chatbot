namespace chatbot2;

public interface IEmbedding
{
    Task<float[]> GetEmbeddingsAsync(string text);
}
