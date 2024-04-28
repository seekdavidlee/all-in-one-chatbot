
using Azure;
using Azure.AI.OpenAI;
using chatbot2.Ingestions;
using chatbot2.Llms;

namespace chatbot2.Embeddings;

public class AzureOpenAIEmbedding : BaseAzureOpenAIClient, IEmbedding
{
    private readonly string deploymentModel;
    private readonly int maxRetry = 3;
    private readonly IngestionReporter ingestionReporter;

    public AzureOpenAIEmbedding(IngestionReporter ingestionReporter)
    {
        deploymentModel = Environment.GetEnvironmentVariable("AzureOpenAIEmbeddingDeploymentModel") ?? throw new Exception("Missing AzureOpenAIEmbeddingDeploymentModel!");
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

    public async Task<List<float[]>> GetEmbeddingsAsync(string[] textList, CancellationToken cancellationToken)
    {
        int retry = 0;
        while (true)
        {
            try
            {
                var response = await Client.GetEmbeddingsAsync(new EmbeddingsOptions(deploymentModel, textList), cancellationToken);
                return response.Value.Data.Select(x => x.Embedding.ToArray()).ToList();
            }
            catch (RequestFailedException ex)
            {
                this.ingestionReporter.IncrementSearchModelsErrors();
                if (retry == maxRetry || ex.Status != 429 || cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                await Task.Delay((retry + 1) * 2000, cancellationToken);
                retry++;
            }
        }
    }
}
