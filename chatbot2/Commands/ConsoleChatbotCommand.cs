using chatbot2.Inferences;
using chatbot2.Llms;
using Microsoft.Extensions.Configuration;

namespace chatbot2.Commands;

public class ConsoleChatbotCommand : ICommandAction
{
    private readonly InferenceWorkflow inferenceWorkflow;

    public ConsoleChatbotCommand(InferenceWorkflow inferenceWorkflow)
    {
        this.inferenceWorkflow = inferenceWorkflow;
    }

    public string Name => "chatbot";

    public async Task ExecuteAsync(IConfiguration argsConfiguration)
    {
        var chatHistory = new ChatHistory { Chats = [] };
        while (true)
        {
            Console.Write("Ask a question.\nUser: ");
            string userInput = Console.ReadLine() ?? "";

            if (userInput == "exit")
            {
                break;
            }

            var chatEntry = new ChatEntry { User = userInput };

            var result = await this.inferenceWorkflow.ExecuteAsync(userInput, chatHistory);

            chatEntry.Bot = result.Text;
            chatEntry.UserTokens = result.PromptTokens;
            chatEntry.BotTokens = result.CompletionTokens;
            chatHistory.Chats.Add(chatEntry);

            Console.WriteLine(result.Text);
            Console.WriteLine("documents found: {0}, total time taken: {1} ms", result.Documents?.Length, result.DurationInMilliseconds);
        }
    }
}
