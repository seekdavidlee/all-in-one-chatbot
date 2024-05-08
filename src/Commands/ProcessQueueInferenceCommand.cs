using Azure.Storage.Queues;
using AIOChatbot.Configurations;
using AIOChatbot.Inferences;
using AIOChatbot.Llms;
using AIOChatbot.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AIOChatbot.Commands;

public class ProcessQueueInferenceCommand : QueueCommandBase<InferenceRequestQueueMessage>
{
    private readonly Dictionary<string, QueueClient> queueClients = [];
    private readonly IConfig config;
    private readonly IEnumerable<IInferenceWorkflow> inferenceWorkflows;

    public ProcessQueueInferenceCommand(
        IConfig config,
        ILogger<ProcessQueueInferenceCommand> logger,
        IEnumerable<IInferenceWorkflow> inferenceWorkflows)
        : base("ingest-queue-inference", config.InferenceQueueName, logger, config)
    {
        this.config = config;
        this.inferenceWorkflows = inferenceWorkflows;
    }

    protected override async Task ProcessMessageAsync(InferenceRequestQueueMessage message, CancellationToken cancellationToken)
    {
        if (message.Query is null || message.ResponseQueueName is null)
        {
            return;
        }

        // guard against circular dependency
        var inferenceWorkflow = inferenceWorkflows.Where(x => x.GetType().Name != nameof(InferenceWorkflowQueue))
            .GetInferenceWorkflow(config);

        var output = await inferenceWorkflow.ExecuteAsync(message.Query, new ChatHistory { Chats = message.ChatHistory }, cancellationToken);

        if (!queueClients.TryGetValue(message.ResponseQueueName, out var queueClient))
        {
            queueClient = new QueueClient(config.AzureQueueConnectionString, message.ResponseQueueName);
            queueClients[message.ResponseQueueName] = queueClient;
        }
        await queueClient.SendMessageAsync(JsonSerializer.Serialize(new InferenceResponseQueueMessage
        {
            CorrelationId = message.CorrelationId,
            Output = output
        }), cancellationToken);
    }
}
