using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;
using chatbot2.Configuration;
using chatbot2.Inferences;

namespace chatbot2.Evals;

public class EvaluationRunner
{
    private readonly ReportRepository reportRepository;
    private readonly InferenceWorkflow inferenceWorkflow;
    private readonly EvaluationMetricWorkflow evaluationMetricWorkflow;
    private readonly IConfig config;
    private readonly ILogger<EvaluationRunner> logger;

    public EvaluationRunner(
        ReportRepository reportRepository,
        InferenceWorkflow inferenceWorkflow,
        EvaluationMetricWorkflow evaluationMetricWorkflow,
        IConfig config,
        ILogger<EvaluationRunner> logger)
    {
        this.reportRepository = reportRepository;
        this.inferenceWorkflow = inferenceWorkflow;
        this.evaluationMetricWorkflow = evaluationMetricWorkflow;
        this.config = config;
        this.logger = logger;
    }

    public async Task RunAsync(int runCounts, string projectId, IEnumerable<GroundTruth> groundTruths, IEnumerable<EvaluationMetricConfig> metrics, CancellationToken cancellationToken)
    {
        string path = $"{projectId}/{DateTime.UtcNow:yyyyMMdd}/{DateTime.UtcNow:hhmmss}";
        await reportRepository.SaveAsync($"{path}/input.json", config);

        logger.LogInformation("starting evaluation runs: {path}", path);

        var blocks = new ActionBlock<Func<Task>>((action) => action(), config.GetDataflowOptions(cancellationToken));

        foreach (var groundTruth in groundTruths)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (groundTruth.Question is null)
            {
                continue;
            }

            for (var i = 0; i < runCounts; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                var index = i;
                await blocks.SendAsync(async () =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    try
                    {
                        logger.LogDebug("running inference for '{question}', run: {count}", groundTruth.Question, index);
                        var answer = await inferenceWorkflow.ExecuteAsync(groundTruth.Question, cancellationToken);
                        if (answer is null)
                        {
                            return;
                        }

                        foreach (var metric in metrics)
                        {
                            logger.LogDebug("running metric {metric}", metric.Name);
                            var metricResult = await evaluationMetricWorkflow.RunAsync(metric, groundTruth, answer, cancellationToken);
                            if (metricResult is null)
                            {
                                return;
                            }

                            await reportRepository.SaveAsync($"{path}/eval-{Guid.NewGuid():N}.json", metricResult);
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "error running inference for '{question}', run: {count}", groundTruth.Question, index);
                    }
                });
            }
        }

        blocks.Complete();
        await blocks.Completion;

        logger.LogInformation("evaluation runs completed for: {path}", path);
    }
}
