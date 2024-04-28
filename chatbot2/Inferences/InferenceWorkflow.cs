using chatbot2.Llms;
using chatbot2.VectorDbs;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace chatbot2.Inferences;

public class InferenceWorkflow
{
    private readonly ILanguageModel languageModel;
    private readonly IVectorDb vectorDb;
    private readonly ILogger<InferenceWorkflow> logger;

    public InferenceWorkflow(IEnumerable<ILanguageModel> languageModels, IEnumerable<IVectorDb> vectorDbs, ILogger<InferenceWorkflow> logger)
    {
        languageModel = languageModels.GetSelectedLanguageModel();
        vectorDb = vectorDbs.GetSelectedVectorDb();
        this.logger = logger;
    }

    private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
    private bool isVectorDbInitialized;
    public async Task<InferenceOutput> ExecuteAsync(string userInput)
    {
        await semaphore.WaitAsync();
        try
        {
            if (!isVectorDbInitialized)
            {
                await vectorDb.InitAsync();
                isVectorDbInitialized = true;
                logger.LogInformation("VectorDb initialized");
            }
        }
        finally
        {
            semaphore.Release();
        }

        var intentPrompt = await Util.GetResourceAsync("DetermineIntent.txt");
        intentPrompt = intentPrompt.Replace("{{$previous_intent}}", "");
        intentPrompt = intentPrompt.Replace("{{$query}}", userInput);

        var intentResponse = await languageModel.GetChatCompletionsAsync(intentPrompt, new LlmOptions());

        if (intentResponse is null)
        {
            throw new Exception("did not get response from llm");
        }

        const string keywordMarker = "Single intents:";
        var findIndex = intentResponse.IndexOf(keywordMarker, StringComparison.OrdinalIgnoreCase);
        if (findIndex < 0)
        {
            throw new Exception("did not find single intent in response");
        }
        intentResponse = intentResponse.Substring(findIndex + keywordMarker.Length);
        var lastIndex = intentResponse.IndexOf("]", 0, StringComparison.OrdinalIgnoreCase);
        intentResponse = intentResponse.Substring(0, lastIndex + 1);
        var parsedIntents = JsonSerializer.Deserialize<string[]>(intentResponse);
        if (parsedIntents is null)
        {
            throw new Exception("response did not deserialize properly");
        }

        if (parsedIntents.Length == 0)
        {
            parsedIntents = [userInput];
        }


        List<IndexedDocument> results = [];
        foreach (var intent in parsedIntents)
        {
            var docResults = (await vectorDb.SearchAsync(intent)).ToArray();
            results.AddRange(docResults);
        }

        var resultsArr = results.ToArray();
        var replyPrompt = await Util.GetResourceAsync("DetermineReply.txt");
        replyPrompt = replyPrompt.Replace("{{$conversation}}", "");
        replyPrompt = replyPrompt.Replace("{{$documentation}}", resultsArr.FullBody());
        replyPrompt = replyPrompt.Replace("{{$user_query}}", userInput);

        Stopwatch stopwatch = new();
        var replyResponse = await languageModel.GetChatCompletionsAsync(replyPrompt, new LlmOptions());
        stopwatch.Stop();
        if (replyResponse is null)
        {
            throw new Exception("did not get response from llm");
        }

        return new InferenceOutput
        {
            Text = replyResponse,
            DurationInMilliseconds = stopwatch.ElapsedMilliseconds,
            Documents = resultsArr
        };
    }
}
