using Azure.AI.OpenAI;

namespace chatbot2.Llms;

public class AzureOpenAIClient : BaseAzureOpenAIClient, ILanguageModel
{
    private readonly string deploymentName;
    public AzureOpenAIClient()
    {
        deploymentName = Environment.GetEnvironmentVariable("AzureOpenAILLMDeploymentModel") ?? throw new Exception("Missing AzureOpenAILLMDeploymentModel!");
    }

    public async Task<string> GetChatCompletionsAsync(string text)
    {
        ChatRequestUserMessage chatMessage = new(text);

        var options = new ChatCompletionsOptions
        {
            DeploymentName = deploymentName,
            MaxTokens = 256,
            Temperature = 0,
        };
        options.Messages.Add(chatMessage);

        var response = await Client.GetChatCompletionsAsync(options);

        return response.Value.Choices[0].Message.Content;
    }
}

