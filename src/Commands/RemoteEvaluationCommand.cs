using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using AIOChatbot.Configurations;
using AIOChatbot.Evals;
using AIOChatbot.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AIOChatbot.Commands;

public class RemoteEvaluationCommand : ICommandAction
{
    private readonly ILogger logger;
    private readonly IConfig config;
    private readonly EvaluationRunner evaluationRunner;
    private readonly ReportRepository reportRepository;

    public RemoteEvaluationCommand(ILogger<RemoteEvaluationCommand> logger, IConfig config, EvaluationRunner evaluationRunner, ReportRepository reportRepository)
    {
        this.logger = logger;
        this.config = config;
        this.evaluationRunner = evaluationRunner;
        this.reportRepository = reportRepository;
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

        string mode = argsConfiguration["remote-evals-mode"] ?? "";
        QueueClient? queueClient = null;
        if (mode == "Queue")
        {
            queueClient = new(config.AzureQueueConnectionString, config.EvaluationQueueName);
        }

        var groundTruthsContainer = new BlobContainerClient(config.AzureStorageConnectionString, config.GroundTruthStorageName);

        List<EvaluationMetricConfig> metrics = [];
        var projectContainer = new BlobContainerClient(config.AzureStorageConnectionString, config.ProjectStorageName);
        await foreach (var blobMetric in projectContainer.GetBlobsAsync(prefix: $"{evalConfig.ProjectId}/{evalConfig.ExperimentId}", cancellationToken: cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (blobMetric.Name.EndsWith(".json"))
            {
                var blob = new BlobClient(config.AzureStorageConnectionString, config.ProjectStorageName, blobMetric.Name);
                var response = await blob.DownloadContentAsync(cancellationToken);
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

        string projPath = $"{evalConfig.ProjectId}/{DateTime.UtcNow:yyyyMMdd}/{DateTime.UtcNow:hhmmss}";
        await reportRepository.SaveAsync($"{projPath}/input.json", config);

        List<GroundTruth> groundTruths = [];
        await foreach (var gt in groundTruthsContainer.GetBlobsAsync(prefix: $"{evalConfig.ProjectId}/{evalConfig.GroundTruthVersionId}"))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (string.IsNullOrEmpty(mode))
            {
                var blob = new BlobClient(config.AzureStorageConnectionString, config.GroundTruthStorageName, gt.Name);
                var response = await blob.DownloadContentAsync(cancellationToken);
                groundTruths.Add(JsonSerializer.Deserialize<GroundTruth>(response.Value.Content) ?? throw new Exception("unable to deserialize groundtruth blob"));
            }
            else
            {
                if (queueClient is null)
                {
                    continue;
                }

                foreach (var m in metrics)
                {
                    await queueClient.SendMessageAsync(JsonSerializer.Serialize(new GroundTruthQueueMessage
                    {
                        GroudTruthName = gt.Name,
                        GroudTruthStorageName = config.GroundTruthStorageName,
                        RunCount = evalConfig.RunCount,
                        ProjectPath = projPath,
                        Metric = m
                    }), cancellationToken);
                }
            }

            if (top is not null && top == groundTruths.Count)
            {
                break;
            }
        }

        if (string.IsNullOrEmpty(mode))
        {
            await evaluationRunner.RunAsync(evalConfig.RunCount.Value, projPath, groundTruths, metrics, cancellationToken);
        }
    }
}
