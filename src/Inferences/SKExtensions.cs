using AIOChatbot.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace AIOChatbot.Inferences;

public static class SKExtensions
{
    public static ServiceCollection AddSK(this ServiceCollection services)
    {
        // Semantic Kernel is only used for Azure OpenAI Chat Completion
        services.AddSingleton((sp) =>
        {
            var config = sp.GetRequiredService<IConfig>();

            var kernelBuilder = Kernel.CreateBuilder()
                            .AddAzureOpenAIChatCompletion(config.AzureOpenAILLMDeploymentModel, config.AzureOpenAIEndpoint, config.AzureOpenAIKey);

            return kernelBuilder.Build();
        });

        return services;
    }
}
