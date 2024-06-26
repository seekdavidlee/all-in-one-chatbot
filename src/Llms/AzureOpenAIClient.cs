﻿using Azure.AI.OpenAI;
using AIOChatbot.Configurations;

namespace AIOChatbot.Llms;

public class AzureOpenAIClient : BaseAzureOpenAIClient, ILanguageModel
{
    private string? deploymentName;
    public AzureOpenAIClient(IConfig config) : base(config)
    {
        this.config = config;
    }

    private const int DefaultMaxTokens = 4000;
    private readonly IConfig config;

    public async Task<ChatCompletionResponse> GetChatCompletionsAsync(string text, LlmOptions options, ChatHistory? chatHistory = null)
    {
        var chatCompletionsOptions = new ChatCompletionsOptions
        {
            DeploymentName = options.DeploymentName ?? config.AzureOpenAILLMDeploymentModel,
            MaxTokens = options.MaxTokens ?? DefaultMaxTokens,
            Temperature = options.Temperature ?? 0,
        };

        if (options.SystemPrompt is not null)
        {
            chatCompletionsOptions.Messages.Add(new ChatRequestUserMessage(options.SystemPrompt) { Role = ChatRole.System });
        }

        if (chatHistory is not null && chatHistory.Chats is not null)
        {
            // todo: trim chat history if size exceeds
            foreach (var chat in chatHistory.Chats)
            {
                chatCompletionsOptions.Messages.Add(new ChatRequestUserMessage(chat.User) { Role = ChatRole.User });
                chatCompletionsOptions.Messages.Add(new ChatRequestUserMessage(chat.Bot) { Role = ChatRole.Assistant });
            }
        }

        chatCompletionsOptions.Messages.Add(new ChatRequestUserMessage(text) { Role = ChatRole.User });

        var response = await Client.GetChatCompletionsAsync(chatCompletionsOptions);

        return new ChatCompletionResponse(response.Value.Choices[0].Message.Content)
        {
            PromptTokens = response.Value.Usage.PromptTokens,
            CompletionTokens = response.Value.Usage.CompletionTokens,
        };
    }
}

