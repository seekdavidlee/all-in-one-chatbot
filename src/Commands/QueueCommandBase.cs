using Azure.Storage.Queues;
using AIOChatbot.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace AIOChatbot.Commands;

public abstract class QueueCommandBase<T> : ICommandAction
{
    private readonly string commandName;
    private readonly string queueName;
    private readonly ILogger logger;
    private readonly IConfig config;
    private QueueClient? queueClient;
    private QueueClient? poisonQueueClient;
    protected QueueCommandBase(string commandName, string queueName, ILogger logger, IConfig config)
    {
        this.commandName = commandName;
        this.queueName = queueName;
        this.logger = logger;
        this.config = config;
    }

    protected abstract Task ProcessMessageAsync(T message, CancellationToken cancellationToken);

    protected virtual Task InitAsync()
    {
        return Task.CompletedTask;
    }

    public async Task ExecuteAsync(IConfiguration argsConfiguration, CancellationToken cancellationToken)
    {
        if (queueClient is null)
        {
            queueClient = new QueueClient(config.AzureQueueConnectionString, queueName);
        }

        if (poisonQueueClient is null)
        {
            poisonQueueClient = new QueueClient(config.AzureQueueConnectionString, $"poison-{queueName}");
        }

        await InitAsync();

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

            BinaryData? msgBody = null;
            long? dequeueCount = 0;
            string? messageId = null;
            string? popReceipt = null;
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
                dequeueCount = msg.Value.DequeueCount;
                popReceipt = msg.Value.PopReceipt;
                messageId = msg.Value.MessageId;
                msgBody = msg.Value.Body;
                var queueModel = JsonSerializer.Deserialize<T>(msg.Value.Body);
                if (queueModel is not null)
                {
                    lastMessageReceived = DateTime.UtcNow;
                    publishedLastMessageReceived = false;
                    await ProcessMessageAsync(queueModel, cancellationToken);
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

                if (msgBody is not null && dequeueCount >= config.MessageDequeueCount)
                {
                    try
                    {
                        await poisonQueueClient.SendMessageAsync(Encoding.UTF8.GetString(msgBody.ToArray()), cancellationToken);
                        await queueClient.DeleteMessageAsync(messageId, popReceipt, cancellationToken: cancellationToken);
                    }
                    catch
                    {
                        logger.LogError("Error sending message to poison queue");
                    }
                }
            }
            finally
            {
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

    public string Name => commandName;
    public bool LongRunning => true;
}
