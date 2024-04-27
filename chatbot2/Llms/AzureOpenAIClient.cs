using Azure.AI.OpenAI;

namespace chatbot2.Llms;

public class AzureOpenAIClient : BaseAzureOpenAIClient, ILanguageModel
{
    private readonly string deploymentName;
    public AzureOpenAIClient()
    {
        deploymentName = Environment.GetEnvironmentVariable("AzureOpenAILLMDeploymentModel") ?? throw new Exception("Missing AzureOpenAILLMDeploymentModel!");
    }

    private const int DefaultMaxTokens = 256;

    public async Task<string> GetChatCompletionsAsync(string text, LlmOptions options)
    {
        ChatRequestUserMessage chatMessage = new(text);

        var chatCompletionsOptions = new ChatCompletionsOptions
        {
            DeploymentName = options.DeploymentName ?? deploymentName,
            MaxTokens = options.MaxTokens ?? DefaultMaxTokens,
            Temperature = options.Temperature ?? 0,
        };
        chatCompletionsOptions.Messages.Add(chatMessage);

        var response = await Client.GetChatCompletionsAsync(chatCompletionsOptions);

        return response.Value.Choices[0].Message.Content;
    }
}

