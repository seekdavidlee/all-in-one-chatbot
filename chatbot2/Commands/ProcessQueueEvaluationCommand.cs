using Azure.Storage.Blobs.Specialized;
using chatbot2.Configuration;
using chatbot2.Evals;
using chatbot2.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace chatbot2.Commands;

public class ProcessQueueEvaluationCommand : QueueCommandBase<GroundTruthQueueMessage>
{
    private readonly ILogger<ProcessQueueEvaluationCommand> logger;
    private readonly IConfig config;
    private readonly EvaluationRunner evaluationRunner;


    public ProcessQueueEvaluationCommand(ILogger<ProcessQueueEvaluationCommand> logger, IConfig config, EvaluationRunner evaluationRunner)
        : base("ingest-queue-evals", config.EvaluationQueueName, logger, config)
    {
        this.logger = logger;
        this.config = config;
        this.evaluationRunner = evaluationRunner;
    }

    protected override async Task ProcessMessageAsync(GroundTruthQueueMessage message, CancellationToken cancellationToken)
    {
        if (message.RunCount is null || message.Metric is null || message.ProjectPath is null)
        {
            return;
        }
        var blob = new BlockBlobClient(config.AzureStorageConnectionString, message.GroudTruthStorageName, message.GroudTruthName);
        var cnt = await blob.DownloadContentAsync(cancellationToken);
        var gt = JsonSerializer.Deserialize<GroundTruth>(cnt.Value.Content);
        if (gt is null)
        {
            return;
        }

        for (var i = 0; i < message.RunCount; i++)
        {
            await evaluationRunner.RunAsync(message.ProjectPath, gt, [message.Metric], i, cancellationToken);
        }
    }
}
