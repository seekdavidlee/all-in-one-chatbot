using Azure.AI.OpenAI;
using Azure;

namespace chatbot2.Llms;

public abstract class BaseAzureOpenAIClient
{
    private readonly OpenAIClient openAIClient;

    protected BaseAzureOpenAIClient()
    {
        var oaiKey = Environment.GetEnvironmentVariable("AzureOpenAIKey") ?? throw new Exception("Missing AzureOpenAIKey!");
        var oaiEndpoint = new Uri(Environment.GetEnvironmentVariable("AzureOpenAIEndpoint") ?? throw new Exception("Missing AzureOpenAIEndpoint!"));

        AzureKeyCredential credentials = new(oaiKey);
        openAIClient = new(oaiEndpoint, credentials);
    }

    protected OpenAIClient Client => openAIClient;
}

