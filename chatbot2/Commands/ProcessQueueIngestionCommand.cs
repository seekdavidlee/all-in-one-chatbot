using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Queues;
using chatbot2.Configuration;
using chatbot2.Ingestions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace chatbot2.Commands;

public class ProcessQueueIngestionCommand : ICommandAction
{
    private readonly IIngestionProcessor ingestionProcessor;
    private readonly ILogger<ProcessQueueIngestionCommand> logger;
    private readonly IConfig config;
    private readonly IVectorDb vectorDb;
    private readonly QueueClient queueClient;

    public string Name => "ingest-queue-processing";

    public ProcessQueueIngestionCommand(IEnumerable<IVectorDb> vectorDbs,
        ILogger<ProcessQueueIngestionCommand> logger,
        IEnumerable<IIngestionProcessor> ingestionProcessors,
        IConfig config)
    {
        ingestionProcessor = ingestionProcessors.GetIngestionProcessor(config);
        this.logger = logger;
        this.config = config;
        vectorDb = vectorDbs.GetSelectedVectorDb();
        queueClient = new(config.AzureQueueConnectionString, config.IngestionQueueName);
    }

    public async Task ExecuteAsync(IConfiguration argsConfiguration, CancellationToken cancellationToken)
    {
        logger.LogInformation("initializing vectordb");
        await vectorDb.InitAsync();

        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                var msg = await queueClient.ReceiveMessageAsync(cancellationToken: cancellationToken);
                var queueModel = JsonSerializer.Deserialize<SearchModelQueueMessage>(Encoding.UTF8.GetString(msg.Value.Body.ToArray()));
                if (queueModel is not null)
                {
                    var blob = new BlockBlobClient(config.AzureStorageConnectionString, config.IngestionQueueStorageName, queueModel.Id.ToString());
                    var cnt = await blob.DownloadContentAsync(cancellationToken);
                    var models = JsonSerializer.Deserialize<List<SearchModelDto>>(Encoding.UTF8.GetString(cnt.Value.Content.ToArray()));

                    if (models is not null)
                    {
                        await ingestionProcessor.ProcessAsync(models, cancellationToken);
                    }
                }
                else
                {
                    logger.LogWarning("Invalid queue message, msg-id: {queueMessageId}", msg.Value.MessageId);
                }

                await queueClient.DeleteMessageAsync(msg.Value.MessageId, msg.Value.PopReceipt, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing queue ingestion");
            }

            await Task.Delay(1000, cancellationToken);
        }
    }
}
