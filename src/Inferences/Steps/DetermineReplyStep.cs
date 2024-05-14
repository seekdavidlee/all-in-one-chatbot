using AIOChatbot.Llms;
using AIOChatbot.VectorDbs;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text;

namespace AIOChatbot.Inferences.Steps;

public class DetermineReplyStep : IInferenceWorkflowStep
{
    private readonly Kernel kernel;
    private readonly ILogger<DetermineReplyStep> logger;

    public DetermineReplyStep(Kernel kernel, ILogger<DetermineReplyStep> logger)
    {
        this.kernel = kernel;
        this.logger = logger;
    }
    public async Task<InferenceWorkflowStepResult> ExecuteAsync(InferenceWorkflowContext context, CancellationToken cancellationToken)
    {
        var stepData = context.GetStepData(nameof(DetermineReplyStep));
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = stepData.TryGetIntInputValue(nameof(OpenAIPromptExecutionSettings.MaxTokens), 800),
            Temperature = stepData.TryGetIntInputValue(nameof(OpenAIPromptExecutionSettings.Temperature), 0),
            TopP = stepData.TryGetIntInputValue(nameof(OpenAIPromptExecutionSettings.TopP), 1)
        };

        var retrievedDocumentsStep = context.GetStepData(nameof(RetrievedDocumentsStep));

        var args = new KernelArguments(executionSettings)
        {
            { "conversation", context.ChatHistory is not null? GetConversationHistory(context.ChatHistory): string.Empty },
            { "documentation", GetDocumentation(retrievedDocumentsStep.GetOutputValue<List<IndexedDocument>>(RetrievedDocumentsStep.SEARCH_RESULTS_KEY)) },
            { "user_query", context.UserInput }
        };

        string determineReplyPrompt = await Util.GetResourceAsync("DetermineReply.txt");

        int promptTokens = 0;
        int completionTokens = 0;
        var result = await kernel.InvokePromptAsync(determineReplyPrompt, args, cancellationToken: cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            return new InferenceWorkflowStepResult(false, "operation cancelled");
        }

        if (result is null)
        {
            return new InferenceWorkflowStepResult(false, "no response from llm");
        }

        context.BotResponse = result.ToString();

        if (result.Metadata is not null && result.Metadata.TryGetValue(USAGE_KEY, out var usageOut) && usageOut is CompletionsUsage usage)
        {
            promptTokens = usage.PromptTokens;
            completionTokens = usage.CompletionTokens;
            stepData.AddStepOutput(USAGE_KEY, usage);
        }

        return new InferenceWorkflowStepResult(true)
        {
            TotalCompletionTokens = completionTokens,
            TotalPromptTokens = promptTokens
        };
    }

    private static string GetConversationHistory(ChatHistory chatHistory)
    {
        if (chatHistory.Chats is null)
        {
            return string.Empty;
        }

        StringBuilder sb = new();
        foreach (var chat in chatHistory.Chats)
        {
            sb.AppendLine(SPEAKER_KEY);
            sb.AppendLine($"message: {chat.User}");
            sb.AppendLine(ASSISTANT_KEY);
            sb.AppendLine($"message: {chat.Bot}");
        }

        return sb.ToString();
    }
    const string SPEAKER_KEY = "speaker: user";
    const string ASSISTANT_KEY = "speaker: assistant";
    const string USAGE_KEY = "Usage";

    private static string GetDocumentation(List<IndexedDocument> docs)
    {
        StringBuilder sb = new();
        for (var i = 0; i < docs.Count; i++)
        {
            var m = docs[i];
            sb.AppendLine($"doc[{i}]\n{m.Text}\n");
        }

        return sb.ToString();
    }

    public Dictionary<string, string> CreateInputs()
    {
        var dic = new Dictionary<string, string>
        {
            { nameof(OpenAIPromptExecutionSettings.MaxTokens), "800" },
            { nameof(OpenAIPromptExecutionSettings.Temperature), "0" },
            { nameof(OpenAIPromptExecutionSettings.TopP), "1" }
        };
        return dic;
    }
}
