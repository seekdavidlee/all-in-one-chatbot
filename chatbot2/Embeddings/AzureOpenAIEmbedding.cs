
using Azure;
using Azure.AI.OpenAI;
using chatbot2.Llms;

namespace chatbot2.Embeddings;

public class AzureOpenAIEmbedding : BaseAzureOpenAIClient, IEmbedding
{
    private readonly string deploymentModel;
    private readonly int maxRetry = 3;
    public AzureOpenAIEmbedding()
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
    }

    public async Task<List<float[]>> GetEmbeddingsAsync(string[] textList)
    {
        int retry = 0;
        while (true)
        {
            try
            {
                var response = await Client.GetEmbeddingsAsync(new EmbeddingsOptions(deploymentModel, textList));
                return response.Value.Data.Select(x => x.Embedding.ToArray()).ToList();
            }
            catch (RequestFailedException ex)
            {
                if (retry == maxRetry || ex.Status != 429)
                {
                    throw;
                }

                await Task.Delay((retry + 1) * 2000);
                retry++;
            }
        }
    }
}
