using chatbot2.Evals;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace chatbot2.Commands;

public class LocalEvaluationCommand : ICommandAction
{
    private readonly EvaluationRunner evaluationRunner;
    private readonly ReportRepository reportRepository;
    private readonly GroundTruthIngestion groundTruthIngestion;

    private readonly ILogger<LocalEvaluationCommand> logger;


    public LocalEvaluationCommand(
        EvaluationRunner evaluationRunner,
        ReportRepository reportRepository,
        GroundTruthIngestion groundTruthIngestion,
        ILogger<LocalEvaluationCommand> logger)
    {
        this.evaluationRunner = evaluationRunner;
        this.reportRepository = reportRepository;
        this.groundTruthIngestion = groundTruthIngestion;
        this.logger = logger;
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

        string projPath = $"{config.ProjectId}/{DateTime.UtcNow:yyyyMMdd}/{DateTime.UtcNow:hhmmss}";
        foreach (var group in groups)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await evaluationRunner.RunAsync(config.RunCount.Value, projPath, group.Value, config.Metrics, cancellationToken);
        }

        logger.LogInformation("evaluation runs completed for: {path}", path);
    }
}
