using chatbot2.Inferences;
using Microsoft.Extensions.Configuration;

namespace chatbot2.Commands;

public class ConsoleInferenceCommand : ICommandAction
{
    private readonly InferenceWorkflow inferenceWorkflow;

    public ConsoleInferenceCommand(InferenceWorkflow inferenceWorkflow)
    {
        this.inferenceWorkflow = inferenceWorkflow;
    }

    public string Name => "chatbot";

    public async Task ExecuteAsync(IConfiguration argsConfiguration)
    {
        while (true)
        {
            Console.Write("Ask a question.\nUser: ");
            string userInput = Console.ReadLine() ?? "";

            if (userInput == "exit")
            {
                break;
            }

            var result = await this.inferenceWorkflow.ExecuteAsync(userInput);
            Console.WriteLine(result.Text);
            Console.WriteLine("documents found: {0}, total time taken: {1} ms", result.Documents?.Length, result.DurationInMilliseconds);
        }
    }
}
