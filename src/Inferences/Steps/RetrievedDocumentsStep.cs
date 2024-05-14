
using AIOChatbot.VectorDbs;

namespace AIOChatbot.Inferences.Steps;

public class RetrievedDocumentsStep : IInferenceWorkflowStep
{
    public const string SEARCH_RESULTS_KEY = "SearchResults";
    private readonly IEnumerable<IVectorDb> vectorDbs;
    public RetrievedDocumentsStep(IEnumerable<IVectorDb> vectorDbs)
    {
        this.vectorDbs = vectorDbs;
    }

    private IVectorDb? vectorDb;
    public IVectorDb GetSelectedVectorDb()
    {
        vectorDb ??= vectorDbs.GetSelectedVectorDb();

        return vectorDb;
    }

    const int DefaultNumberOfResults = 5;
    const double MinScore = 0.7;

    public async Task<InferenceWorkflowStepResult> ExecuteAsync(InferenceWorkflowContext context, CancellationToken cancellationToken)
    {
        var stepData = context.GetStepData(nameof(RetrievedDocumentsStep));
        var numberOfResult = stepData.TryGetIntInputValue(nameof(SearchParameters.NumberOfResults), DefaultNumberOfResults);
        var minScore = stepData.TryGetDoubleInputValue(nameof(SearchParameters.MinScore), MinScore);
        var vectorDb = GetSelectedVectorDb();

        var determineIntentStep = context.GetStepData(nameof(DetermineIntentStep));
        var results = await vectorDb.SearchAsync(determineIntentStep.GetOutputValue<string[]>(DetermineIntentStep.INTENTS_KEY),
            new SearchParameters { NumberOfResults = numberOfResult, MinScore = minScore },
            cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            return new InferenceWorkflowStepResult(false, "operation cancelled");
        }

        stepData.AddStepOutput(SEARCH_RESULTS_KEY, results.Documents);

        return new InferenceWorkflowStepResult(true)
        {
            Documents = [.. results.Documents],
            TotalEmbeddingTokens = results.TotalTokens
        };
    }

    public Dictionary<string, string> CreateInputs()
    {
        var dic = new Dictionary<string, string>
        {
            { nameof(SearchParameters.NumberOfResults), DefaultNumberOfResults.ToString() },
            { nameof(SearchParameters.MinScore), MinScore.ToString() },
        };
        return dic;
    }
}
