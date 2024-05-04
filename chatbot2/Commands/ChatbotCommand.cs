﻿using chatbot2.Configuration;
using chatbot2.Inferences;
using chatbot2.Llms;
using Microsoft.Extensions.Configuration;

namespace chatbot2.Commands;

public class ChatbotCommand : ICommandAction
{
    private readonly IInferenceWorkflow inferenceWorkflow;

    public ChatbotCommand(IConfig config, IEnumerable<IInferenceWorkflow> inferenceWorkflows)
    {
        this.inferenceWorkflow = inferenceWorkflows.GetInferenceWorkflow(config);
    }

    public string Name => "chatbot";

    public async Task ExecuteAsync(IConfiguration argsConfiguration, CancellationToken cancellationToken)
    {
        Console.WriteLine("Chatbot is ready. Type in a question or type 'exit' to quit.");
        var chatHistory = new ChatHistory { Chats = [] };
        while (true)
        {
            Console.Write("User: ");
            string userInput = Console.ReadLine() ?? "";

            if (userInput == "exit")
            {
                break;
            }

            var chatEntry = new ChatEntry { User = userInput };

            var result = await this.inferenceWorkflow.ExecuteAsync(userInput, chatHistory, cancellationToken);

            chatEntry.Bot = result.Text;
            chatEntry.UserTokens = result.PromptTokens;
            chatEntry.BotTokens = result.CompletionTokens;
            chatHistory.Chats.Add(chatEntry);

            Console.WriteLine($"Bot: {result.Text}");
            Console.WriteLine("documents found: {0}, total time taken: {1} ms", result.Documents?.Length, result.DurationInMilliseconds);
        }
    }
}
