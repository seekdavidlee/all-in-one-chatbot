/*

namespace AIOChatbot.Inferences.Steps;

public class RetrievedDocumentsStep : IInferenceWorkflowStep
{
    public const string SEARCH_RESULTS_KEY = "search_results";
    private readonly IEnumerable<IVectorDb> vectorDbs;
    public RetrievedDocumentsStep(IEnumerable<IVectorDb> vectorDbs)
    {
        this.vectorDbs = vectorDbs;
    }

    public async Task<bool> ExecuteAsync(InferenceWorkflowContext context, CancellationToken cancellationToken)
    {
        var vectorDb = vectorDbs.GetSelectedVectorDb();
        var results = vectorDb.SearchAsync(GetIntentText(context), cancellationToken);

        List<SearchResultModel> searchResults = [];
        await foreach (var result in results)
        {
            searchResults.Add(result.SearchResultModel);
        }

        var dict = new Dictionary<string, object>
        {
            [SEARCH_RESULTS_KEY] = searchResults
        };
        context.Steps.Add(nameof(RetrievedDocumentsStep), dict);

        return true;
    }

    public static string GetIntentText(InferenceWorkflowContext context)
    {
        var src = context.Steps[nameof(DetermineIntentStep)];
        var intents = (string[])src[DetermineIntentStep.INTENTS_KEY];

        var intent = intents.SingleOrDefault();

        if (intent is null)
        {
            var input = (PromptflowRequest)context.Steps[nameof(InputStep)][InputStep.PROMPTFLOW_REQUEST_KEY];
            if (input.Query is null)
            {
                throw new InvalidOperationException("Input Query is null");
            }

            return input.Query;
        }
        return intent;
    }


}
*/