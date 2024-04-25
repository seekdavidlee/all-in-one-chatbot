using LLama.Common;
using LLama;

namespace chatbot2.Embeddings;

public class LocalEmbedding : IEmbedding
{
    private readonly LLamaEmbedder embedder;
    public LocalEmbedding()
    {
        string modelPath = Environment.GetEnvironmentVariable("EmbeddingFilePath") ?? throw new Exception("Missing EmbeddingFilePath!"); // change it to your own model path.
        var @params = new ModelParams(modelPath) { EmbeddingMode = true };
        using var weights = LLamaWeights.LoadFromFile(@params);
        embedder = new LLamaEmbedder(weights, @params);
    }

    public Task<float[]> GetEmbeddingsAsync(string text)
    {
        return embedder.GetEmbeddings(text);
    }
}
