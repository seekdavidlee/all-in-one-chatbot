using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Queues;
using chatbot2.Configuration;
using chatbot2.Evals;
using chatbot2.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace chatbot2.Commands;

public class ProcessQueueEvaluationCommand : ICommandAction
{
    private readonly ILogger<ProcessQueueEvaluationCommand> logger;
    private readonly IConfig config;
    private readonly EvaluationRunner evaluationRunner;
    private readonly QueueClient queueClient;
    public string Name => "ingest-queue-evals";

    public ProcessQueueEvaluationCommand(ILogger<ProcessQueueEvaluationCommand> logger, IConfig config, EvaluationRunner evaluationRunner)
    {
        this.logger = logger;
        this.config = config;
        this.evaluationRunner = evaluationRunner;
        queueClient = new(config.AzureQueueConnectionString, config.EvaluationQueueName);
    }

    public async Task ExecuteAsync(IConfiguration argsConfiguration, CancellationToken cancellationToken)
    {
        logger.LogInformation("started listening for records...");

        DateTime? lastMessageReceived = null;
        bool publishedLastMessageReceived = false;

        DateTime lastReported = DateTime.UtcNow;
        int processed = 0;
        int errored = 0;

        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                var msg = await queueClient.ReceiveMessageAsync(cancellationToken: cancellationToken);
                if (msg.Value is null)
                {
                    if (!publishedLastMessageReceived)
                    {
                        if (lastMessageReceived is not null)
                        {
                            logger.LogInformation("Last message received at {lastMessageReceived}", lastMessageReceived);
                            publishedLastMessageReceived = true;
                        }
                    }
                    await Task.Delay(TimeSpan.FromMilliseconds(this.config.IngestionQueuePollingInterval), cancellationToken);
                    continue;
                }
                var gtModel = JsonSerializer.Deserialize<GroundTruthQueueMessage>(msg.Value.Body);
                if (gtModel is not null)
                {
                    lastMessageReceived = DateTime.UtcNow;
                    publishedLastMessageReceived = false;
                    var blob = new BlockBlobClient(config.AzureStorageConnectionString, gtModel.GroudTruthStorageName, gtModel.GroudTruthName);
                    var cnt = await blob.DownloadContentAsync(cancellationToken);
                    var gt = JsonSerializer.Deserialize<GroundTruth>(cnt.Value.Content);
                    if (gt is null || gtModel.RunCount is null || gtModel.Metric is null || gtModel.ProjectPath is null)
                    {
                        continue;
                    }

                    for (var i = 0; i < gtModel.RunCount; i++)
                    {
                        await evaluationRunner.RunAsync(gtModel.ProjectPath, gt, [gtModel.Metric], i, cancellationToken);
                    }
                }
                else
                {
                    logger.LogWarning("Invalid queue message, msg-id: {queueMessageId}", msg.Value.MessageId);
                }

                await queueClient.DeleteMessageAsync(msg.Value.MessageId, msg.Value.PopReceipt, cancellationToken: cancellationToken);
                processed++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing queue ingestion");
                errored++;
            }

            var span = DateTime.UtcNow - lastReported;
            if (span.TotalSeconds > config.IngestionReportEveryXSeconds)
            {
                lastReported = DateTime.UtcNow;
                logger.LogInformation("Processed {processed} records, errored {errored} records", processed, errored);
                processed = 0;
                errored = 0;
            }
        }
    }
}
