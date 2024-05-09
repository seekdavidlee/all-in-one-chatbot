using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;

namespace AIOChatbot.Inferences.Steps;

public class DetermineIntentStep(Kernel kernel) : IInferenceWorkflowStep
{
    public async Task<bool> ExecuteAsync(InferenceWorkflowContext context, CancellationToken cancellationToken)
    {
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 800,
            Temperature = 0,
            TopP = 1
        };

        string determineIntentPrompt = await Util.GetResourceAsync("DetermineIntent.txt");

        var args = new KernelArguments(executionSettings)
        {
            { "previous_intent", GetPreviousIntent(context) },
            { "query", context.UserInput }
        };

        var result = await kernel.InvokePromptAsync(determineIntentPrompt, args, cancellationToken: cancellationToken);
        
        var intents = GetIntents(result.ToString());

        var dict = new Dictionary<string, object>
        {
            [INTENTS_KEY] = intents
        };
        context.Steps.Add(nameof(DetermineIntentStep), dict);
        return true;
    }

    public const string INTENTS_KEY = "intents";

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
}
