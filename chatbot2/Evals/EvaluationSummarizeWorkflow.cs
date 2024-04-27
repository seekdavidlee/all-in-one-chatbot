using Microsoft.Extensions.Logging;

namespace chatbot2.Evals;

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
        await foreach (var result in reportRepository.GetAsync<EvaluationMetricResult>(path))
        {
            if (result.MetricName is null)
            {
                logger.LogWarning("MetricName is missing");
                continue;
            }

            if (result.Results is null)
            {
                logger.LogWarning("Results is missing");
                continue;
            }

            if (!metrics.TryGetValue(result.MetricName, out var metricSummary))
            {
                metricSummary = new MetricSummary
                {
                    Name = result.MetricName,
                    TotalDurationInMilliseconds = 0,
                    TotalRuns = 0,
                    TotalScore = 0,
                    TotalFailedRuns = 0
                };
            }

            foreach (var item in result.Results)
            {
                if (item.Score is null)
                {
                    logger.LogWarning("Score is missing for {MetricName}", result.MetricName);
                    metricSummary.TotalFailedRuns += 1;
                }
                else
                {
                    metricSummary.TotalScore += item.Score.Value;
                }

                metricSummary.TotalDurationInMilliseconds += item.DurationInMilliseconds;
                metricSummary.TotalRuns += 1;
            }

            metrics[result.MetricName] = metricSummary;
        }

        foreach (var key in metrics.Keys)
        {
            var name = $"{path}/{key}.json";
            logger.LogInformation("Saving {metricFilePath}", name);
            await reportRepository.SaveAsync(name, metrics[key]);
        }
    }
}
