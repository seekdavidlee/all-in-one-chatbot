using Azure.Storage.Blobs;
using chatbot2.Configuration;
using chatbot2.Evals;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace chatbot2.Commands;

public class RemoteEvaluationCommand : ICommandAction
{
    private readonly ILogger logger;
    private readonly IConfig config;
    private readonly EvaluationRunner evaluationRunner;

    public RemoteEvaluationCommand(ILogger<RemoteEvaluationCommand> logger, IConfig config, EvaluationRunner evaluationRunner)
    {
        this.logger = logger;
        this.config = config;
        this.evaluationRunner = evaluationRunner;
    }

    public string Name => "remote-evals";

    public async Task ExecuteAsync(IConfiguration argsConfiguration, CancellationToken cancellationToken)
    {
        var configFilePath = argsConfiguration["config-filepath"];
        if (configFilePath is null)
        {
            logger.LogWarning("config-filepath argument is missing");
            return;
        }

        var evalConfig = JsonSerializer.Deserialize<EvaluationConfig>(File.ReadAllText(configFilePath));

        if (evalConfig is null)
        {
            logger.LogWarning("unable to deserialize {filepath}", configFilePath);
            return;
        }

        if (evalConfig.RunCount is null)
        {
            logger.LogWarning("config RunCount is not set");
            return;
        }

        if (evalConfig.ProjectId is null)
        {
            logger.LogWarning("config ProjectId is not set");
            return;
        }

        var groundTruthsContainer = new BlobContainerClient(config.AzureStorageConnectionString, config.GroundTruthStorageName);

        List<EvaluationMetricConfig> metrics = [];
        var projectContainer = new BlobContainerClient(config.AzureStorageConnectionString, config.ProjectStorageName);
        await foreach (var blobMetric in projectContainer.GetBlobsAsync(prefix: $"{evalConfig.ProjectId}/{evalConfig.ExperimentId}"))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (blobMetric.Name.EndsWith(".json"))
            {
                var blob = new BlobClient(config.AzureStorageConnectionString, config.ProjectStorageName, blobMetric.Name);
                var response = await blob.DownloadContentAsync();
                var metric = JsonSerializer.Deserialize<EvaluationMetricConfig>(response.Value.Content) ?? throw new Exception("unable to deserialize metric blob");

                int lastIndex = blobMetric.Name.LastIndexOf("/");
                metric.PromptFilePath = $"blob:{config.ProjectStorageName}/{blobMetric.Name[..lastIndex]}/{metric.PromptFilePath}";
                metrics.Add(metric);
            }
        }

        var pickTopGroundTruths = argsConfiguration["pick-top-groundtruths"];
        int? top = null;
        if (pickTopGroundTruths is not null && int.TryParse(pickTopGroundTruths, out int pickTopGroundTruthsTop))
        {
            top = pickTopGroundTruthsTop;
        }
        List<GroundTruth> groundTruths = [];
        await foreach (var gt in groundTruthsContainer.GetBlobsAsync(prefix: $"{evalConfig.ProjectId}/{evalConfig.GroundTruthVersionId}"))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var blob = new BlobClient(config.AzureStorageConnectionString, config.GroundTruthStorageName, gt.Name);
            var response = await blob.DownloadContentAsync();
            groundTruths.Add(JsonSerializer.Deserialize<GroundTruth>(response.Value.Content) ?? throw new Exception("unable to deserialize groundtruth blob"));

            if (top is not null && top == groundTruths.Count)
            {
                break;
            }
        }

        await evaluationRunner.RunAsync(evalConfig.RunCount.Value, evalConfig.ProjectId, groundTruths, metrics, cancellationToken);
    }
}
