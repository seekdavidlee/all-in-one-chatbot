using Azure.Storage.Queues;
using chatbot2.Configuration;
using chatbot2.Inferences;
using chatbot2.Llms;
using chatbot2.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace chatbot2.Commands;

public class ProcessQueueInferenceCommand : QueueCommandBase<InferenceRequestQueueMessage>
{
    private readonly Dictionary<string, QueueClient> queueClients = [];
    private readonly IConfig config;
    private readonly IInferenceWorkflow inferenceWorkflow;

    public ProcessQueueInferenceCommand(
        IConfig config,
        ILogger<ProcessQueueInferenceCommand> logger,
        IEnumerable<IInferenceWorkflow> inferenceWorkflows)
        : base("ingest-queue-inference", config.InferenceQueueName, logger, config)
    {
        this.config = config;

        // guard against circular dependency
        this.inferenceWorkflow = inferenceWorkflows.Where(x => x.GetType().Name != nameof(InferenceWorkflowQueue))
            .GetInferenceWorkflow(config);
    }

    protected override async Task ProcessMessageAsync(InferenceRequestQueueMessage message, CancellationToken cancellationToken)
    {
        if (message.Query is null || message.ResponseQueueName is null)
        {
            return;
        }
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
