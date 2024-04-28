using chatbot2.Evals;
using chatbot2.Inferences;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks.Dataflow;

namespace chatbot2.Commands;

public class EvaluationCommand : ICommandAction
{
    private readonly ReportRepository reportRepository;
    private readonly GroundTruthIngestion groundTruthIngestion;
    private readonly EvaluationMetricWorkflow evaluationMetricWorkflow;
    private readonly InferenceWorkflow inferenceWorkflow;
    private readonly ILogger<EvaluationCommand> logger;
    private readonly int concurrency = 1;
    public EvaluationCommand(
        ReportRepository reportRepository,
        GroundTruthIngestion groundTruthIngestion,
        EvaluationMetricWorkflow evaluationMetricWorkflow,
        InferenceWorkflow inferenceWorkflow,
        ILogger<EvaluationCommand> logger)
    {
        this.reportRepository = reportRepository;
        this.groundTruthIngestion = groundTruthIngestion;
        this.evaluationMetricWorkflow = evaluationMetricWorkflow;
        this.inferenceWorkflow = inferenceWorkflow;
        this.logger = logger;

        var concurrencyStr = Environment.GetEnvironmentVariable("Concurrency");
        if (concurrencyStr is not null && int.TryParse(concurrencyStr, out int concurencyInt))
        {
            concurrency = concurencyInt;
        }
    }

    public string Name => "evals";

    public async Task ExecuteAsync(IConfiguration argsConfiguration)
    {
        var configFilePath = argsConfiguration["config-filepath"];
        if (configFilePath is null)
        {
            logger.LogWarning("config-filepath argument is missing");
            return;
        }

        var config = JsonSerializer.Deserialize<EvaluationConfig>(File.ReadAllText(configFilePath));

        if (config is null)
        {
            logger.LogWarning("unable to deserialize {filepath}", configFilePath);
            return;
        }

        if (config.Metrics is null)
        {
            logger.LogWarning("config metrics is not set");
            return;
        }

        var groundTruths = await groundTruthIngestion.RunAsync(config);
        if (!groundTruths.Any())
        {
            logger.LogWarning("no ground truths found per configuration file-path {configFilePath}", configFilePath);
            return;
        }

        var pickTopGroundTruths = argsConfiguration["pick-top-groundtruths"];
        int? top = null;
        if (pickTopGroundTruths is not null && int.TryParse(pickTopGroundTruths, out int pickTopGroundTruthsTop))
        {
            top = pickTopGroundTruthsTop;
        }

        var random = new Random();
        var groups = groundTruths.GroupBy(x => x.DataSource).ToDictionary(g => g.Key ?? throw new Exception("key is invalid"),
            g => top is not null && g.Count() > top ?
            g.OrderBy(x => random.Next()).Take(top.Value) :
            g.ToList());

        string path = $"{DateTime.UtcNow:yyyyMMdd}/{DateTime.UtcNow:hhmmss}";
        await reportRepository.SaveAsync($"{path}/input.json", config);

        logger.LogInformation("starting evaluation runs: {path}", path);

        ExecutionDataflowBlockOptions options = new()
        {
            MaxDegreeOfParallelism = concurrency,
            TaskScheduler = TaskScheduler.Default
        };

        var blocks = new ActionBlock<Func<Task>>((action) => action(), options);

        foreach (var group in groups)
        {
            foreach (var groundTruth in group.Value)
            {
                if (groundTruth.Question is null)
                {
                    continue;
                }

                for (var i = 0; i < config.RunCount; i++)
                {
                    await blocks.SendAsync(async () =>
                    {
                        try
                        {
                            logger.LogDebug("running inference for '{question}', run: {count}", groundTruth.Question, i);
                            var answer = await inferenceWorkflow.ExecuteAsync(groundTruth.Question);
                            if (answer is null)
                            {
                                return;
                            }

                            foreach (var metric in config.Metrics)
                            {
                                logger.LogDebug("running metric {metric}", metric.Name);
                                var metricResult = await evaluationMetricWorkflow.RunAsync(metric, groundTruth, answer);
                                if (metricResult is null)
                                {
                                    return;
                                }

                                await reportRepository.SaveAsync($"{path}/eval-{Guid.NewGuid():N}.json", metricResult);
                            }
                        }
                        catch (Exception e)
                        {
                            logger.LogError(e, "error running inference for '{question}', run: {count}", groundTruth.Question, i);
                        }
                    });
                }
            }
        }

        blocks.Complete();
        await blocks.Completion;

        logger.LogInformation("evaluation runs completed for: {path}", path);
    }
}
