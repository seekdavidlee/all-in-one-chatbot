
using Azure;
using Azure.AI.OpenAI;
using AIOChatbot.Configurations;
using AIOChatbot.Ingestions;
using AIOChatbot.Logging;
using System.Diagnostics;

namespace AIOChatbot.Embeddings;

public class AzureOpenAIEmbedding : IEmbedding
{
    private readonly int maxRetry = 3;
    private readonly IngestionReporter ingestionReporter;
    private readonly IConfig config;
    private OpenAIClient[]? openAIClients;
    private readonly object lockObj = new();
    private string[]? deploymentModels;

    public AzureOpenAIEmbedding(IngestionReporter ingestionReporter, IConfig config)
    {
        var maxRetryStr = Environment.GetEnvironmentVariable("AzureOpenAIEmbeddingMaxRetry");
        if (maxRetryStr is not null)
        {
            if (int.TryParse(maxRetryStr, out int envMaxRetry))
            {
                maxRetry = envMaxRetry;
            }
        }

        this.ingestionReporter = ingestionReporter;
        this.config = config;
    }
    private int currentIndex;
    private (OpenAIClient Client, string deploymentName) GetNextClient()
    {
        lock (lockObj)
        {
            if (openAIClients is null)
            {
                var deploymentsStr = config.AzureOpenAIEmbeddings;
                var deployments = deploymentsStr.Split(';');

                deploymentModels = new string[deployments.Length];

                openAIClients = deployments.Select((x, i) =>
                {
                    var parts = x.Split(',');
                    if (parts.Length != 3)
                    {
                        throw new Exception($"Invalid AzureOpenAIEmbeddings format at index {i}!");
                    }

                    deploymentModels[i] = parts[2];

                    AzureKeyCredential credentials = new(parts[1]);
                    return new OpenAIClient(new Uri(parts[0]), credentials);
                }).ToArray();
            }

            if (openAIClients.Length == 0)
            {
                throw new InvalidOperationException("No clients available");
            }

            currentIndex = (currentIndex + 1) % openAIClients.Length;
            return (openAIClients[currentIndex], deploymentModels is not null ? deploymentModels[currentIndex] : throw new Exception("deploymentModels is null"));
        }
    }

    public async Task<EmbeddingResult> GetEmbeddingsAsync(string[] textList, CancellationToken cancellationToken)
    {
        int retry = 0;
        while (true)
        {
            try
            {
                var (Client, DeploymentName) = GetNextClient();

                Stopwatch sw = new();
                sw.Start();
                var response = await Client.GetEmbeddingsAsync(new EmbeddingsOptions(DeploymentName, textList), cancellationToken);
                sw.Stop();

                DiagnosticServices.RecordEmbeddingTokens(
                    response.Value.Usage.TotalTokens, sw.ElapsedMilliseconds, textList.Length, DeploymentName);
                DiagnosticServices.RecordEmbeddingTokensPerSecond(
                    response.Value.Usage.TotalTokens / sw.Elapsed.TotalSeconds, sw.ElapsedMilliseconds, textList.Length, DeploymentName);

                this.ingestionReporter.IncrementEmbeddingTokensProcessed(response.Value.Usage.TotalTokens);
                return new EmbeddingResult
                {
                    TotalTokens = response.Value.Usage.TotalTokens,
                    DurationInMilliseconds = sw.ElapsedMilliseconds,
                    Vectors = response.Value.Data.Select(x => x.Embedding.ToArray()).ToList()
                };
            }
            catch (RequestFailedException ex)
            {
                if (retry == maxRetry || ex.Status != 429 || cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                await Task.Delay((retry + 1) * 1000, cancellationToken);
                retry++;
            }
        }
    }
}
