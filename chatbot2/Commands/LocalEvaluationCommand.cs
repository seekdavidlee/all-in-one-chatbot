using chatbot2.Configuration;
using chatbot2.Evals;
using chatbot2.Inferences;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks.Dataflow;

namespace chatbot2.Commands;

public class LocalEvaluationCommand : ICommandAction
{
    private readonly EvaluationRunner evaluationRunner;
    private readonly ReportRepository reportRepository;
    private readonly GroundTruthIngestion groundTruthIngestion;
    private readonly EvaluationMetricWorkflow evaluationMetricWorkflow;
    private readonly InferenceWorkflow inferenceWorkflow;
    private readonly ILogger<LocalEvaluationCommand> logger;
    private readonly IConfig cbConfig;

    public LocalEvaluationCommand(
        EvaluationRunner evaluationRunner,
        ReportRepository reportRepository,
        GroundTruthIngestion groundTruthIngestion,
        EvaluationMetricWorkflow evaluationMetricWorkflow,
        InferenceWorkflow inferenceWorkflow,
        ILogger<LocalEvaluationCommand> logger,
        IConfig cbConfig)
    {
        this.evaluationRunner = evaluationRunner;
        this.reportRepository = reportRepository;
        this.groundTruthIngestion = groundTruthIngestion;
        this.evaluationMetricWorkflow = evaluationMetricWorkflow;
        this.inferenceWorkflow = inferenceWorkflow;
        this.logger = logger;
        this.cbConfig = cbConfig;
    }

    public string Name => "local-evals";

    public async Task ExecuteAsync(IConfiguration argsConfiguration, CancellationToken cancellationToken)
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

        if (config.RunCount is null)
        {
            logger.LogWarning("config RunCount is not set");
            return;
        }

        if (config.ProjectId is null)
        {
            logger.LogWarning("config ProjectId is not set");
            return;
        }

        var groundTruths = await groundTruthIngestion.RunAsync(config, cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

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

        foreach (var group in groups)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await evaluationRunner.RunAsync(config.RunCount.Value, config.ProjectId, group.Value, config.Metrics, cancellationToken);
        }

        logger.LogInformation("evaluation runs completed for: {path}", path);
    }
}
