using Azure.Storage.Blobs;
using AIOChatbot.Configurations;
using AIOChatbot.Evals;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace AIOChatbot.Commands;

public class ImportGroundTruthsCommand : ICommandAction
{
    private readonly ILogger<ImportGroundTruthsCommand> logger;
    private readonly GroundTruthIngestion groundTruthIngestion;
    private readonly IConfig config;

    public ImportGroundTruthsCommand(ILogger<ImportGroundTruthsCommand> logger, GroundTruthIngestion groundTruthIngestion, IConfig config)
    {
        this.logger = logger;
        this.groundTruthIngestion = groundTruthIngestion;
        this.config = config;
    }
    public string Name => "import-ground-truths";
    public bool LongRunning => false;

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

        if (groundTruthsConfig.GroundTruthsMapping is null)
        {
            logger.LogWarning("missing GroundTruthsMapping");
            return;
        }

        var groundTruthsGroups = await groundTruthIngestion.RunAsync(groundTruthsConfig, cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (!groundTruthsGroups.Any())
        {
            logger.LogWarning("no ground truths found per configuration file-path {configFilePath}", configFilePath);
            return;
        }

        if (groundTruthsConfig.ProjectId is null || groundTruthsConfig.GroundTruthVersionId is null)
        {
            logger.LogWarning("ProjectId or GroundTruthVersionId is not set");
            return;
        }

        // report every few seconds
        DateTime lastReport = DateTime.UtcNow;
        for (var i = 0; i < groundTruthsGroups.Count(); i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var groundTruthsGroup = groundTruthsGroups.ElementAt(i);
            List<GroundTruth> history = [];
            foreach(var groundTruth in groundTruthsGroup.GroundTruths)
            {
                groundTruth.PreviousGroundTruths = history;
                var path = $"{groundTruthsConfig.ProjectId}\\{groundTruthsConfig.GroundTruthVersionId}\\{Guid.NewGuid():N}.json";
                var blob = new BlobClient(config.AzureStorageConnectionString, config.GroundTruthStorageName, path);
                await blob.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(groundTruth))), cancellationToken);

                history.Add(groundTruth);
            }

            var elasped = DateTime.UtcNow - lastReport;
            if (elasped.TotalSeconds > config.IngestionReportEveryXSeconds)
            {
                logger.LogInformation("current import progress: {current}/{total}", i, groundTruthsGroups.Count());
                lastReport = DateTime.UtcNow;
            }
        }

        logger.LogInformation("imported {groundTruthsTotal} ground-truths", groundTruthsGroups.Count());
    }
}
