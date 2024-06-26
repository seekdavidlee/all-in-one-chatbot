﻿using LLama.Common;
using LLama;
using System.Text;

namespace AIOChatbot.Llms;

public class LocalLLM : ILanguageModel
{
    private LLamaContext? context;
    private readonly string? modelPath;
    private readonly SemaphoreSlim semaphore = new(1, 1);

    public LocalLLM()
    {
        modelPath = Environment.GetEnvironmentVariable("ModelFilePath");
    }

    public async Task<ChatCompletionResponse> GetChatCompletionsAsync(string text, LlmOptions options, ChatHistory? chatHistory = null)
    {
        if (modelPath is null)
        {
            throw new Exception("Missing ModelFilePath!");
        }
        await semaphore.WaitAsync();
        try
        {
            if (context is null)
            {
                var parameters = new ModelParams(modelPath)
                {
                    ContextSize = 1024, // The longest length of chat as memory.
                    GpuLayerCount = 5, // How many layers to offload to GPU. Please adjust it according to your GPU memory.
                };
                using var model = LLamaWeights.LoadFromFile(parameters);
                context = model.CreateContext(parameters);
            }
        }
        finally
        {
            semaphore.Release();
        }

        InferenceParams inferenceParams = new()
        {
            MaxTokens = 256, // No more than 256 tokens should appear in answer. Remove it if antiprompt is enough for control.
            AntiPrompts = ["User:"], // Stop generation once antiprompts appear.
            Temperature = options.Temperature ?? 0,
            //TopK = 10,
            //TopP = 0.1F,
        };

        StringBuilder sb = new();
        var instructExecutor = new InstructExecutor(context);
        var answers = instructExecutor.InferAsync(text, inferenceParams);
        await foreach (var answer in answers)
        {
            sb.Append(answer);
        }

        return new ChatCompletionResponse(sb.ToString());
    }
}
