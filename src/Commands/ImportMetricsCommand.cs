using Azure.Storage.Blobs;
using AIOChatbot.Configurations;
using AIOChatbot.Evals;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace AIOChatbot.Commands;

public class ImportMetricsCommand : ICommandAction
{
    private readonly ILogger<ImportMetricsCommand> logger;
    private readonly IConfig config;

    public ImportMetricsCommand(ILogger<ImportMetricsCommand> logger, IConfig config)
    {
        this.logger = logger;
        this.config = config;
    }

    public string Name => "import-metrics";

    public async Task ExecuteAsync(IConfiguration argsConfiguration, CancellationToken cancellationToken)
    {
        var configFilePath = argsConfiguration["config-filepath"];
        if (configFilePath is null)
        {
            logger.LogWarning("config-filepath argument is missing");
            return;
        }

        var groundTruthsConfig = JsonSerializer.Deserialize<EvaluationConfig>(File.ReadAllText(configFilePath));

        if (groundTruthsConfig is null)
        {
            logger.LogWarning("unable to deserialize {filepath}", configFilePath);
            return;
        }

        if (groundTruthsConfig.Metrics is null)
        {
            logger.LogWarning("config metrics is not set");
            return;
        }

        if (groundTruthsConfig.ProjectId is null || groundTruthsConfig.ExperimentId is null)
        {
            logger.LogWarning("ProjectId or ExperimentId is not set");
            return;
        }

        foreach (var metric in groundTruthsConfig.Metrics)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (metric.PromptFilePath is null)
            {
                continue;
            }

            var fileInfo = new FileInfo(metric.PromptFilePath);
            var promptPath = $"{groundTruthsConfig.ProjectId}\\{groundTruthsConfig.ExperimentId}\\{fileInfo.Name}";
            var prompt = new BlobClient(config.AzureStorageConnectionString, config.ProjectStorageName, promptPath);
            await prompt.UploadAsync(metric.PromptFilePath, cancellationToken);

            var path = $"{groundTruthsConfig.ProjectId}\\{groundTruthsConfig.ExperimentId}\\{metric.Name}.json";
            metric.PromptFilePath = fileInfo.Name;
            var metricBlob = new BlobClient(config.AzureStorageConnectionString, config.ProjectStorageName, path);

            await metricBlob.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(metric))), cancellationToken);
        }

        logger.LogInformation("imported {metricsTotal} metrics", groundTruthsConfig.Metrics.Count());
    }
}
