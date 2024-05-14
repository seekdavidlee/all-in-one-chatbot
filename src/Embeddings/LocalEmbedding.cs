using LLama.Common;
using LLama;
using System.Diagnostics;

namespace AIOChatbot.Embeddings;

public class LocalEmbedding : IEmbedding
{
    private LLamaEmbedder? embedder;
    private string? modelPath;
    private readonly SemaphoreSlim semaphore = new(1, 1);

    public async Task<EmbeddingResult> GetEmbeddingsAsync(string[] textList, CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        var sw = new Stopwatch();
        sw.Start();
        try
        {
            modelPath ??= Environment.GetEnvironmentVariable("EmbeddingFilePath") ?? throw new Exception("Missing EmbeddingFilePath!"); // change it to your own model path.
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
            sw.Stop();
        }
        List<float[]> list = [];
        foreach (var text in textList)
        {
            list.Add(await embedder.GetEmbeddings(text, cancellationToken));
        }

        // todo: calculate totaltokens

        return new EmbeddingResult 
        { 
            Vectors = list, 
            DurationInMilliseconds = sw.ElapsedMilliseconds 
        };
    }
}
