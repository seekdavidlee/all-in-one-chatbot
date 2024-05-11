namespace AIOChatbot.Inferences.Steps;

public class RetrievedDocumentsStep : IInferenceWorkflowStep
{
    public const string SEARCH_RESULTS_KEY = "search_results";
    private readonly IEnumerable<IVectorDb> vectorDbs;
    public RetrievedDocumentsStep(IEnumerable<IVectorDb> vectorDbs)
    {
        this.vectorDbs = vectorDbs;
    }

    private IVectorDb? vectorDb;
    public IVectorDb GetSelectedVectorDb()
    {
        if (vectorDb is null)
        {
            vectorDb = vectorDbs.GetSelectedVectorDb();
        }

        return vectorDb;
    }

    public async Task<InferenceWorkflowStepResult> ExecuteAsync(InferenceWorkflowContext context, CancellationToken cancellationToken)
    {
        var stepData = context.GetStepData(nameof(RetrievedDocumentsStep));

        var vectorDb = GetSelectedVectorDb();

        var determineIntentStep = context.GetStepData(nameof(DetermineIntentStep));
        var results = await vectorDb.SearchAsync(determineIntentStep.GetOutputValue<string[]>(DetermineIntentStep.INTENTS_KEY), cancellationToken);

        stepData.AddStepOutput(SEARCH_RESULTS_KEY, results.ToList());

        return new InferenceWorkflowStepResult(true);
    }
}
