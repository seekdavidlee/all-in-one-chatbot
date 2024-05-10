using AIOChatbot.Llms;
using AIOChatbot.VectorDbs;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace AIOChatbot.Inferences;

public class InferenceWorkflow : IInferenceWorkflow
{
    private readonly IEnumerable<ILanguageModel> languageModels;
    private readonly IEnumerable<IVectorDb> vectorDbs;
    private readonly ILogger<InferenceWorkflow> logger;

    public InferenceWorkflow(IEnumerable<ILanguageModel> languageModels, IEnumerable<IVectorDb> vectorDbs, ILogger<InferenceWorkflow> logger)
    {
        this.languageModels = languageModels;
        this.vectorDbs = vectorDbs;
        this.logger = logger;
    }

    private readonly SemaphoreSlim semaphore = new(1, 1);
    private bool isVectorDbInitialized;
    public async Task<InferenceOutput> ExecuteAsync(string userInput, ChatHistory? chatHistory, CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync();
        try
        {
            if (!isVectorDbInitialized)
            {
                var vectorDb1 = vectorDbs.GetSelectedVectorDb();
                await vectorDb1.InitAsync();
                isVectorDbInitialized = true;
                logger.LogInformation("VectorDb initialized");
            }
        }
        finally
        {
            semaphore.Release();
        }

        var output = new InferenceOutput { Steps = [] };

        Stopwatch stopwatch = new();
        stopwatch.Start();

        var step0 = new InferenceStepData { Name = "chatHistory" };
        step0.Outputs["Count"] = chatHistory?.Chats?.Count.ToString() ?? "-1";
        output.Steps.Add(step0);

        var intentPrompt = await Util.GetResourceAsync("DetermineIntent.txt");
        intentPrompt = intentPrompt.Replace("{{$previous_intent}}", "");
        intentPrompt = intentPrompt.Replace("{{$query}}", userInput);

        var languageModel = languageModels.GetSelectedLanguageModel();
        var chatCompletionResponse = await languageModel.GetChatCompletionsAsync(intentPrompt, new LlmOptions());

        var step1 = new InferenceStepData { Name = "DetermineIntent" };
        step1.Outputs["CompletionTokens"] = chatCompletionResponse?.CompletionTokens?.ToString() ?? "-1";
        step1.Outputs["PromptTokens"] = chatCompletionResponse?.PromptTokens?.ToString() ?? "-1";
        output.Steps.Add(step1);

        var intentResponse = chatCompletionResponse?.Text ?? throw new Exception("did not get response from llm");
        const string keywordMarker = "Single intents:";
        var findIndex = intentResponse.IndexOf(keywordMarker, StringComparison.OrdinalIgnoreCase);
        if (findIndex < 0)
        {
            throw new Exception("did not find single intent in response");
        }
        intentResponse = intentResponse[(findIndex + keywordMarker.Length)..];
        var lastIndex = intentResponse.IndexOf("]", 0, StringComparison.OrdinalIgnoreCase);
        intentResponse = intentResponse[..(lastIndex + 1)];
        var parsedIntents = JsonSerializer.Deserialize<string[]>(intentResponse) ?? throw new Exception("response did not deserialize properly");
        if (parsedIntents.Length == 0)
        {
            parsedIntents = [userInput];
        }

        var vectorDb = vectorDbs.GetSelectedVectorDb();
        List<IndexedDocument> results = [];
        for (int i = 0; i < parsedIntents.Length; i++)
        {
            var intent = parsedIntents[i];
            step1.Outputs[$"parsedIntents_{i}"] = intent;
            var docResults = (await vectorDb.SearchAsync(intent, cancellationToken)).ToArray();

            step1.Outputs[$"parsedIntents_{i}_docs_count"] = docResults.Length.ToString();
            results.AddRange(docResults);
        }

        var resultsArr = results.ToArray();
        var replyPrompt = await Util.GetResourceAsync("DetermineReply.txt");
        replyPrompt = replyPrompt.Replace("{{$conversation}}", chatHistory is not null ? chatHistory.FullBody() : "");
        replyPrompt = replyPrompt.Replace("{{$documentation}}", resultsArr.FullBody());
        replyPrompt = replyPrompt.Replace("{{$user_query}}", userInput);

        var replyResponse = await languageModel.GetChatCompletionsAsync(replyPrompt, new LlmOptions());
        stopwatch.Stop();
        if (replyResponse is null)
        {
            throw new Exception("did not get response from llm");
        }

        output.Text = replyResponse.Text;
        output.DurationInMilliseconds = stopwatch.ElapsedMilliseconds;
        output.Documents = resultsArr;
        output.CompletionTokens = replyResponse.CompletionTokens;
        output.PromptTokens = replyResponse.PromptTokens;

        return output;
    }
}
