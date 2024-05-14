using Azure.AI.OpenAI;
using Azure;
using AIOChatbot.Configurations;

namespace AIOChatbot.Llms;

public abstract class BaseAzureOpenAIClient
{
    private OpenAIClient? openAIClient;
    private readonly IConfig config;

    protected BaseAzureOpenAIClient(IConfig config)
    {
        this.config = config;
    }

    protected OpenAIClient Client
    {
        get
        {
            if (openAIClient is null)
            {
                var oaiKey = config.AzureOpenAIKey;
                var oaiEndpoint = new Uri(Environment.GetEnvironmentVariable("AzureOpenAIEndpoint") ?? throw new Exception("Missing AzureOpenAIEndpoint!"));

                AzureKeyCredential credentials = new(oaiKey);
                openAIClient = new(oaiEndpoint, credentials);
            }

            return openAIClient;
        }
    }
}

