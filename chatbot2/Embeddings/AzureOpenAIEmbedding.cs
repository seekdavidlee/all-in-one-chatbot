
using Azure.AI.OpenAI;
using chatbot2.Llms;

namespace chatbot2.Embeddings;

public class AzureOpenAIEmbedding : BaseAzureOpenAIClient, IEmbedding
{
    private readonly string deploymentModel;
    public AzureOpenAIEmbedding()
    {
        deploymentModel = Environment.GetEnvironmentVariable("AzureOpenAIEmbeddingDeploymentModel") ?? throw new Exception("Missing AzureOpenAIEmbeddingDeploymentModel!");
    }

    public async Task<float[]> GetEmbeddingsAsync(string text)
    {
        var response = await Client.GetEmbeddingsAsync(new EmbeddingsOptions(deploymentModel, [text]));
        return response.Value.Data[0].Embedding.ToArray();
    }
}
