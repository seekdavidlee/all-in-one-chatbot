using AIOChatbot.Configuration;
using AIOChatbot.Inferences;
using AIOChatbot.Llms;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace AIOChatbot.Commands;

public class ChatbotCommand : ICommandAction
{
    private readonly IEnumerable<IInferenceWorkflow> inferenceWorkflows;
    private readonly IConfig config;

    public ChatbotCommand(IConfig config, IEnumerable<IInferenceWorkflow> inferenceWorkflows)
    {
        this.inferenceWorkflows = inferenceWorkflows;
        this.config = config;
    }

    public string Name => "chatbot";

    public async Task ExecuteAsync(IConfiguration argsConfiguration, CancellationToken cancellationToken)
    {
        var inferenceWorkflow = inferenceWorkflows.GetInferenceWorkflow(config);
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

            Stopwatch sw = new();
            sw.Start();
            var result = await inferenceWorkflow.ExecuteAsync(userInput, chatHistory, cancellationToken);
            sw.Stop();
            chatEntry.Bot = result.Text;
            chatEntry.UserTokens = result.PromptTokens;
            chatEntry.BotTokens = result.CompletionTokens;
            chatHistory.Chats.Add(chatEntry);

            Console.WriteLine($"Bot: {result.Text}");
            Console.WriteLine("documents found: {0}, inferenceWorkflow total time taken: {1} ms", result.Documents?.Length, result.DurationInMilliseconds);
            Console.WriteLine("total time taken: {0} ms", sw.ElapsedMilliseconds);
        }
    }
}
