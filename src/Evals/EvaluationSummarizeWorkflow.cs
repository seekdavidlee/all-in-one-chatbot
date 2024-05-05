using Microsoft.Extensions.Logging;

namespace AIOChatbot.Evals;

public class EvaluationSummarizeWorkflow
{
    private readonly ReportRepository reportRepository;
    private readonly ILogger<EvaluationSummarizeWorkflow> logger;

    public EvaluationSummarizeWorkflow(ReportRepository reportRepository, ILogger<EvaluationSummarizeWorkflow> logger)
    {
        this.reportRepository = reportRepository;
        this.logger = logger;
    }

    public async Task CreateAsync(string path)
    {
        Dictionary<string, MetricSummary> metrics = [];
        await foreach (var (Item, BlobName) in reportRepository.GetAsync<EvaluationMetricResult>($"{path}/eval-"))
        {
            if (Item.MetricName is null)
            {
                logger.LogWarning("MetricName is missing for {blobName}", BlobName);
                continue;
            }

            if (Item.Results is null)
            {
                logger.LogWarning("Results is missing for {blobName}", BlobName);
                continue;
            }

            if (!metrics.TryGetValue(Item.MetricName, out var metricSummary))
            {
                metricSummary = new MetricSummary
                {
                    Name = Item.MetricName,
                    TotalDurationInMilliseconds = 0,
                    TotalRuns = 0,
                    TotalScore = 0,
                    TotalFailedRuns = 0,
                    FailedRunBlobNames = [],
                };
            }

            foreach (var item in Item.Results)
            {
                if (item.Score is null)
                {
                    logger.LogWarning("Score is missing for {MetricName}, blob: {BlobName} at index {Index}", Item.MetricName, BlobName, item.Index);
                    metricSummary.TotalFailedRuns += 1;
                    metricSummary.FailedRunBlobNames?.Add(BlobName);
                }
                else
                {
                    metricSummary.TotalScore += item.Score.Value;
                }

                metricSummary.TotalDurationInMilliseconds += item.DurationInMilliseconds;
                metricSummary.TotalRuns += 1;
            }

            metrics[Item.MetricName] = metricSummary;
        }

        foreach (var key in metrics.Keys)
        {
            var name = $"{path}/summary-{key}.json";
            logger.LogInformation("Saving {metricFilePath}", name);
            await reportRepository.SaveAsync(name, metrics[key]);
        }
    }
}
