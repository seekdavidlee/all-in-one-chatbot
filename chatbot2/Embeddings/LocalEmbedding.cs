using LLama.Common;
using LLama;

namespace chatbot2.Embeddings;

public class LocalEmbedding : IEmbedding
{
    private LLamaEmbedder? embedder;
    private readonly string? modelPath;
    private readonly SemaphoreSlim semaphore = new(1, 1);

    public LocalEmbedding()
    {
        modelPath = Environment.GetEnvironmentVariable("EmbeddingFilePath") ?? throw new Exception("Missing EmbeddingFilePath!"); // change it to your own model path.
    }

    public async Task<List<float[]>> GetEmbeddingsAsync(string[] textList)
    {
        if (modelPath is null)
        {
            throw new Exception("Missing EmbeddingFilePath!");
        }
        await semaphore.WaitAsync();
        try
        {
            if (embedder is null)
            {
                var @params = new ModelParams(modelPath) { EmbeddingMode = true };
                using var weights = LLamaWeights.LoadFromFile(@params);
                embedder = new LLamaEmbedder(weights, @params);
            }
        }
        finally
        {
            semaphore.Release();
        }
        List<float[]> list = new();
        foreach (var text in textList)
        {
            list.Add(await embedder.GetEmbeddings(text));
        }
        return list;
    }
}
