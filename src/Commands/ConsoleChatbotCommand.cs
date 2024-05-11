using AIOChatbot.Configurations;
using AIOChatbot.Inferences;
using AIOChatbot.Llms;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace AIOChatbot.Commands;

public class ConsoleChatbotCommand : ICommandAction
{
    private readonly IEnumerable<IInferenceWorkflow> inferenceWorkflows;
    private readonly IConfig config;

    public ConsoleChatbotCommand(IConfig config, IEnumerable<IInferenceWorkflow> inferenceWorkflows)
    {
        this.inferenceWorkflows = inferenceWorkflows;
        this.config = config;
    }

    public string Name => "chatbot";

    public bool LongRunning => true;

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
            if (result.ErrorMessage is not null)
            {
                Console.WriteLine($"Inference error: {result.ErrorMessage}");
            }
            else
            {
                chatEntry.Bot = result.Text;

                //chatEntry.Intent = 
                chatEntry.UserTokens = result.TotalPromptTokens;
                chatEntry.BotTokens = result.TotalCompletionTokens;
                chatHistory.Chats.Add(chatEntry);

                Console.WriteLine($"Bot: {result.Text}");
            }

            Console.WriteLine("documents found: {0}, inferenceWorkflow total time taken: {1} ms", result.Documents?.Length, result.DurationInMilliseconds);
            Console.WriteLine("total time taken: {0} ms", sw.ElapsedMilliseconds);
        }
    }
}
