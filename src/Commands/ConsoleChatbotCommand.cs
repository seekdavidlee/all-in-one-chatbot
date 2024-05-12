using AIOChatbot.Configurations;
using AIOChatbot.Inferences;
using AIOChatbot.Llms;
using AIOChatbot.VectorDbs;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System.Diagnostics;
using System.Text;

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

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Chatbot is ready. Type in a question or type '/exit' to quit. Type '/help' to see available commands.");
        Console.ResetColor();

        var chatHistory = new ChatHistory { Chats = [] };
        List<InferenceOutput> outputs = [];

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("User: ");
            Console.ResetColor();

            string userInput = Console.ReadLine() ?? "";

            if (userInput == "/exit")
            {
                break;
            }

            if (userInput.StartsWith("/"))
            {
                ProcessCommand(userInput[1..], outputs);
            }
            else
            {
                await ProcessUserQueryAsync(userInput, inferenceWorkflow, chatHistory, outputs, cancellationToken);
            }
        }
    }

    private void ProcessCommand(string unparsedArgs, List<InferenceOutput> outputs)
    {
        var args = unparsedArgs.Split(' ');
        if (args.Length == 0)
        {
            return;
        }

        string command = args[0];

        if (command == "help")
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine("/help - show this help message");
            Console.WriteLine("/clear - clear console");
            Console.WriteLine("/show-output [index] - show output at specific chat history index");
            Console.WriteLine("/show-output-docs [output index] [docs index] - show specific doc by index of an output");
            return;
        }

        if (command == "clear")
        {
            Console.Clear();
            return;
        }

        if (command == "show-output")
        {
            ProcessShowOutputCommand(args, outputs);
            return;
        }

        if (command == "show-output-docs")
        {
            ProcessShowOutputDocCommand(args, outputs);
            return;
        }

        Console.WriteLine("Unknown command. Type '/help' to see available commands.");
    }

    private void ProcessShowOutputDocCommand(string[] args, List<InferenceOutput> outputs)
    {
        if (args.Length < 2)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Please provide an output index");
            Console.ResetColor();
            return;
        }

        if (!int.TryParse(args[1], out int index))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("output Index must be a number");
            Console.ResetColor();
            return;
        }

        if (index < 0 || index >= outputs.Count)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"output Index out of range. Count is: {outputs.Count}");
            Console.ResetColor();
            return;
        }

        var output = outputs[index];
        if (output.Documents is null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No documents in output");
            Console.ResetColor();
            return;
        }

        if (args.Length < 3)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Please provide a document index");
            Console.ResetColor();
            return;
        }

        if (!int.TryParse(args[2], out int docIndex))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Document index must be a number");
            Console.ResetColor();
            return;
        }

        if (docIndex < 0 || docIndex >= output.Documents.Length)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Document index out of range. Count is: {output.Documents.Length}");
            Console.ResetColor();
            return;
        }

        var doc = output.Documents[docIndex];
        Console.WriteLine($"doc[{docIndex}]");
        Console.WriteLine(doc.ToString());
    }

    private static void ProcessShowOutputCommand(string[] args, List<InferenceOutput> outputs)
    {
        if (args.Length < 2)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Please provide an index");
            Console.ResetColor();
            return;
        }

        if (!int.TryParse(args[1], out int index))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Index must be a number");
            Console.ResetColor();
            return;
        }

        if (index < 0 || index >= outputs.Count)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Index out of range. Count is: {outputs.Count}");
            Console.ResetColor();
            return;
        }

        var output = outputs[index];
        Console.WriteLine(GetOutputString(output));
    }

    private static string GetOutputString(InferenceOutput output)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Text: {output.Text}");
        sb.AppendLine($"Duration: {output.DurationInMilliseconds} ms");

        if (output.Intents is not null)
        {
            sb.AppendLine($"Intents: {string.Join(", ", output.Intents)}");
        }

        sb.AppendLine($"Documents count: {output.Documents?.Length}");
        sb.AppendLine($"TotalPromptTokens: {output.TotalPromptTokens}");
        sb.AppendLine($"TotalCompletionTokens: {output.TotalCompletionTokens}");
        sb.AppendLine($"TotalEmbeddingTokens: {output.TotalEmbeddingTokens}");
        return sb.ToString();
    }

    private async Task ProcessUserQueryAsync(
        string userInput,
        IInferenceWorkflow inferenceWorkflow,
        ChatHistory chatHistory,
        List<InferenceOutput> outputs,
        CancellationToken cancellationToken)
    {
        var chatEntry = new ChatEntry { User = userInput };

        Stopwatch sw = new();
        sw.Start();
        var result = await inferenceWorkflow.ExecuteAsync(userInput, chatHistory, cancellationToken);
        sw.Stop();
        if (result.ErrorMessage is not null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Inference error: {result.ErrorMessage}");
            Console.ResetColor();
        }
        else
        {
            outputs.Add(result);

            chatEntry.Bot = result.Text;

            chatEntry.Intents = result.Intents;
            chatEntry.UserPromptTokens = result.TotalPromptTokens;
            chatEntry.BotCompletionTokens = result.TotalCompletionTokens;
            chatEntry.BotEmbeddingTokens = result.TotalEmbeddingTokens;
            chatHistory.Chats?.Add(chatEntry);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Bot: ");
            Console.ResetColor();
            Console.WriteLine(result.Text);
        }

        Console.WriteLine("documents found: {0}, inferenceWorkflow total time taken: {1}ms",
            result.Documents?.Length, result.DurationInMilliseconds);
        Console.WriteLine("total time taken: {0}ms", sw.ElapsedMilliseconds);
    }
}
