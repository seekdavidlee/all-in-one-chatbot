/*
using AIOChatbot.Inferences;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace AIOChatbot.Inferences.Steps;

public class DetermineReplyStep : IInferenceWorkflowStep
{
    private readonly Kernel kernel;
    private readonly ILogger<DetermineReplyStep> logger;
    private readonly ISourceService sourceService;

    public DetermineReplyStep(Kernel kernel, ILogger<DetermineReplyStep> logger, ISourceService sourceService)
    {
        this.kernel = kernel;
        this.logger = logger;
        this.sourceService = sourceService;
    }
    public async Task<bool> ExecuteAsync(InferenceWorkflowContext context)
    {
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 800,
            Temperature = 0,
            TopP = 1
        };

        var src = context.Steps[nameof(DetermineIntentStep)];
        var intent = RetrievedDocumentsStep.GetIntentText(context);

        var promptflowRequest = (PromptflowRequest)context.Steps[nameof(InputStep)][InputStep.PROMPTFLOW_REQUEST_KEY];

        var args = new KernelArguments(executionSettings)
        {
            { "conversation", GetConversationHistory(promptflowRequest) },
            { "documentation", GetDocumentation(context) },
            { "user_query", intent }
        };

        string determineReplyPrompt = await Util.GetResourceAsync("DetermineReply.txt");
        var result = await kernel.InvokePromptAsync(determineReplyPrompt, args);

        if (result is null)
        {
            return false;
        }

        var response = new PromptflowResponse
        {
            Reply = result.ToString(),
            CurrentQueryIntent = intent,
            Query = promptflowRequest.Query,
            SearchIntents = intent,
            FetchedDocuments = GetJson(context, sourceService, logger)
        };

        if (result.Metadata is not null && result.Metadata.TryGetValue(USAGE_KEY, out var usageOut) && usageOut is CompletionsUsage usage)
        {
            response.CompletionTokens = usage.CompletionTokens;
            response.PromptTokens = usage.PromptTokens;
        }

        var dict = new Dictionary<string, object>
        {
            [InputStep.PROMPTFLOW_OUTPUT_KEY] = response
        };
        context.Steps.Add(nameof(DetermineReplyStep), dict);

        return true;
    }

    public async IAsyncEnumerable<PromptflowResponse> ExecuteStreamingAsync(WorkflowContext context)
    {
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 800,
            Temperature = 0,
            TopP = 1
        };

        var src = context.Steps[nameof(DetermineIntentStep)];
        var intent = RetrievedDocumentsStep.GetIntentText(context);

        var promptflowRequest = (PromptflowRequest)context.Steps[nameof(InputStep)][InputStep.PROMPTFLOW_REQUEST_KEY];

        var args = new KernelArguments(executionSettings)
        {
            { "conversation", GetConversationHistory(promptflowRequest) },
            { "documentation", GetDocumentation(context) },
            { "user_query", intent }
        };

        string determineReplyPrompt = await Util.GetResourceAsync("DetermineReply.txt");

        Stopwatch sw = new();
        sw.Start();
        var streamingResult = kernel.InvokePromptStreamingAsync(determineReplyPrompt, args);

        int totalCount = 0;
        int counter = 0;

        StringBuilder sb = new();
        StreamingKernelContent? finalResult = null;
        await foreach (var result in streamingResult)
        {
            totalCount++;
            counter++;

            if (totalCount == 0)
            {
                sw.Stop();
                logger.LogInformation("TimeToFirstToken: {TimeToFirstTokenInMilliseconds} ms", sw.ElapsedMilliseconds);
            }

            finalResult = result;

            sb.Append(result.ToString());

            if (counter == 100)
            {
                var response = new PromptflowResponse
                {
                    Reply = sb.ToString(),
                };

                counter = 0;

                // when streaming, there is no USAGE_KEY, hence no CompletionTokens and PromptTokens can be captured.
                yield return response;
            }
        }

        logger.LogInformation("Streaming complete with {totalCount} count. Counter position: {counter}.", totalCount, counter);

        // todo: This does not work, I expect the MetaData to contain USAGE_KEY but its not there...
        if (finalResult is not null && finalResult.Metadata is not null && finalResult.Metadata.TryGetValue(USAGE_KEY, out var usageOut) && usageOut is CompletionsUsage usage)
        {
            logger.LogInformation("Usage completion tokens: {UsageCompletionTokens}", usage.CompletionTokens);
        }

        yield return new PromptflowResponse
        {
            Reply = sb.ToString(),
            CurrentQueryIntent = intent,
            Query = promptflowRequest.Query,
            SearchIntents = intent,
            FetchedDocuments = GetJson(context, sourceService, logger)
        };
    }

    private static string GetConversationHistory(PromptflowRequest promptflowRequest)
    {
        if (promptflowRequest.ChatHistory is not null)
        {
            StringBuilder sb = new();
            foreach (var chat in promptflowRequest.ChatHistory.Reverse())
            {
                if (chat.Inputs is null || chat.Outputs is null)
                {
                    continue;
                }

                sb.AppendLine(SPEAKER_KEY);
                sb.AppendLine($"message: {chat.Inputs.Query}");
                sb.AppendLine(ASSISTANT_KEY);
                sb.AppendLine($"message: {chat.Outputs.Reply}");
            }

            return sb.ToString();
        }
        return string.Empty;
    }
    const string SPEAKER_KEY = "speaker: user";
    const string ASSISTANT_KEY = "speaker: assistant";

    const string USAGE_KEY = "Usage";
    private static string GetJson(WorkflowContext context, ISourceService sourceService, ILogger logger)
    {
        var resultModels = (List<SearchResultModel>)context.Steps[nameof(RetrievedDocumentsStep)][RetrievedDocumentsStep.SEARCH_RESULTS_KEY];
        return JsonSerializer.Serialize(resultModels.Select((x, i) =>
        {
            if (x.Content is null)
            {
                throw new Exception("Unexpected for content to be null");
            }

            if (x.Url is null)
            {
                throw new Exception("Unexpected for url to be null");
            }

            var testTitle = $"Title: {x.Url}";
            string content = x.Content.StartsWith(testTitle) ? x.Content[testTitle.Length..] : x.Content;
            string title = string.IsNullOrEmpty(x.Title) ? x.Url : x.Title;

            if (x.MetaData is not null)
            {
                try
                {
                    var meta = JsonSerializer.Deserialize<FetchedDocumentMeta>(x.MetaData);
                    if (meta is not null && meta.PageNumber is not null)
                    {
                        title = $"{title} page: {meta.PageNumber}";
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to parse meta data for document");
                }

            }

            return new FetchedDocument()
            {
                Key = $"[doc{i}]",
                Content = content,
                Title = title,
                SourceLink = sourceService.GetSourceLink(x),
            };
        }).ToList());
    }

    private static string GetDocumentation(WorkflowContext context)
    {
        var resultModels = (List<SearchResultModel>)context.Steps[nameof(RetrievedDocumentsStep)][RetrievedDocumentsStep.SEARCH_RESULTS_KEY];

        StringBuilder sb = new();
        for (var i = 0; i < resultModels.Count; i++)
        {
            var m = resultModels[i];
            sb.AppendLine($"doc[{i}]\ntitle:{m.Title}\n{m.Content}\n");
        }

        return sb.ToString();
    }
}
*/