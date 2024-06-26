﻿using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using Azure.AI.OpenAI;
using AIOChatbot.Evals;

namespace AIOChatbot.Inferences.Steps;

public class DetermineIntentStep(Kernel kernel, FileCache fileCache) : IInferenceWorkflowStep
{
    const string promptFileUseCacheKey = "PromptFileUseCache";
    const string promptFileKey = "PromptFileSource";
    const string resourceFile = $"{FileCache.PromptsResourcePrefix}DetermineIntent.txt";

    public async Task<InferenceWorkflowStepResult> ExecuteAsync(InferenceWorkflowContext context, CancellationToken cancellationToken)
    {
        var stepData = context.GetStepData(nameof(DetermineIntentStep));

        var executionSettings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = stepData.TryGetIntInputValue(nameof(OpenAIPromptExecutionSettings.MaxTokens), 800),
            Temperature = stepData.TryGetIntInputValue(nameof(OpenAIPromptExecutionSettings.Temperature), 0),
            TopP = stepData.TryGetIntInputValue(nameof(OpenAIPromptExecutionSettings.TopP), 1),
        };

        var args = new KernelArguments(executionSettings)
        {
            { "previous_intent", GetPreviousIntent(context) },
            { "query", context.UserInput }
        };
        
        bool useCache = stepData.TryGetBoolInputValue(promptFileUseCacheKey, true);
        string prompt = await fileCache.GetFileContentAsync(
            stepData.TryGetStringInputValue(promptFileKey, resourceFile), cancellationToken, useCache);

        var result = await kernel.InvokePromptAsync(prompt, args, cancellationToken: cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            return new InferenceWorkflowStepResult(false, "operation cancelled");
        }

        if (result is null)
        {
            return new InferenceWorkflowStepResult(false, "no response from llm");
        }

        int promptTokens = 0;
        int completionTokens = 0;
        if (result.Metadata is not null && result.Metadata.TryGetValue(USAGE_KEY, out object? o) && o is CompletionsUsage usage)
        {
            promptTokens = usage.PromptTokens;
            completionTokens = usage.CompletionTokens;
            stepData.AddStepOutput(USAGE_KEY, usage);
        }

        var intents = GetIntents(result.ToString());

        if (intents.Length == 0)
        {
            return new InferenceWorkflowStepResult(false, "no intents detected, please rephrase your question");
        }

        stepData.AddStepOutput(INTENTS_KEY, intents);

        return new InferenceWorkflowStepResult(true)
        {
            Intents = intents,
            TotalPromptTokens = promptTokens,
            TotalCompletionTokens = completionTokens
        };
    }

    public const string USAGE_KEY = "Usage";
    public const string INTENTS_KEY = "Intents";
    private readonly FileCache fileCache = fileCache;

    private static string GetPreviousIntent(InferenceWorkflowContext context)
    {
        if (context.ChatHistory is not null && context.ChatHistory.Chats is not null)
        {
            var lastChatHistory = context.ChatHistory.Chats.LastOrDefault();
            if (lastChatHistory is not null &&
                lastChatHistory.Intents is not null && lastChatHistory.Intents.Length > 0)
            {
                return string.Join('\n', lastChatHistory.Intents);
            }
        }

        return "None";
    }

    private static string[] GetIntents(string text)
    {
        const string singleIntents = "Single Intents: [";
        int start = text.IndexOf(singleIntents);
        if (start != -1)
        {
            start += singleIntents.Length;
            int end = text.IndexOf(']', start);
            if (end != -1)
            {
                string arrayText = text[start..end];
                return ExtractArrayElements(arrayText);
            }
        }

        return [];
    }

    private static string[] ExtractArrayElements(string arrayText)
    {
        List<string> elements = [];
        int start = 0;

        while (start < arrayText.Length)
        {
            int quoteStart = arrayText.IndexOf('"', start);
            if (quoteStart == -1)
            {
                break;
            }

            int quoteEnd = arrayText.IndexOf('"', quoteStart + 1);
            if (quoteEnd == -1)
            {
                break;
            }

            string element = arrayText.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
            elements.Add(element);

            start = quoteEnd + 1;
        }

        return [.. elements];
    }

    public Dictionary<string, string> CreateInputs()
    {
        var dic = new Dictionary<string, string>
        {
            { nameof(OpenAIPromptExecutionSettings.MaxTokens), "800" },
            { nameof(OpenAIPromptExecutionSettings.Temperature), "0" },
            { nameof(OpenAIPromptExecutionSettings.TopP), "1" },
            { promptFileKey, resourceFile },
            { promptFileUseCacheKey, "true" }
        };
        return dic;
    }
}
