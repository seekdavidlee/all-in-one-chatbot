﻿using Azure.AI.OpenAI;
using Azure;
using AIOChatbot.Configurations;

namespace AIOChatbot.Llms;

public abstract class BaseAzureOpenAIClient
{
    private readonly OpenAIClient openAIClient;

    protected BaseAzureOpenAIClient(IConfig config)
    {
        var oaiKey = config.AzureOpenAIKey;
        var oaiEndpoint = new Uri(Environment.GetEnvironmentVariable("AzureOpenAIEndpoint") ?? throw new Exception("Missing AzureOpenAIEndpoint!"));

        AzureKeyCredential credentials = new(oaiKey);
        openAIClient = new(oaiEndpoint, credentials);
    }

    protected OpenAIClient Client => openAIClient;
}

