namespace chatbot2;

public interface IEmbedding
{
    Task<List<float[]>> GetEmbeddingsAsync(string[] textList);
}
