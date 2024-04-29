
using Azure;
using Azure.AI.OpenAI;
using chatbot2.Configuration;
using chatbot2.Ingestions;
using chatbot2.Llms;

namespace chatbot2.Embeddings;

public class AzureOpenAIEmbedding : IEmbedding
{
    private readonly string[] deploymentModels;
    private readonly int maxRetry = 3;
    private readonly IngestionReporter ingestionReporter;
    private readonly OpenAIClient[] openAIClients;

    public AzureOpenAIEmbedding(IngestionReporter ingestionReporter, IConfig config)
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

        var maxRetryStr = Environment.GetEnvironmentVariable("AzureOpenAIEmbeddingMaxRetry");
        if (maxRetryStr is not null)
        {
            if (int.TryParse(maxRetryStr, out int envMaxRetry))
            {
                maxRetry = envMaxRetry;
            }
        }

        this.ingestionReporter = ingestionReporter;
    }
    private int currentIndex;
    private (OpenAIClient Client, int Index) GetNextClient()
    {
        lock (openAIClients)
        {
            if (openAIClients.Length == 0)
            {
                throw new InvalidOperationException("No clients available");
            }

            currentIndex = (currentIndex + 1) % openAIClients.Length;
            return (openAIClients[currentIndex], currentIndex);
        }
    }

    public async Task<List<float[]>> GetEmbeddingsAsync(string[] textList, CancellationToken cancellationToken)
    {
        int retry = 0;
        while (true)
        {
            try
            {
                var (Client, Index) = GetNextClient();
                var response = await Client.GetEmbeddingsAsync(new EmbeddingsOptions(deploymentModels[Index], textList), cancellationToken);
                return response.Value.Data.Select(x => x.Embedding.ToArray()).ToList();
            }
            catch (RequestFailedException ex)
            {
                this.ingestionReporter.IncrementSearchModelsErrors();
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
