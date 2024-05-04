using Azure.Storage.Queues;
using chatbot2.Configuration;
using chatbot2.Llms;
using chatbot2.Models;
using System.Text.Json;

namespace chatbot2.Inferences;

public class InferenceWorkflowQueue : IInferenceWorkflow
{
    private readonly QueueClient requestQueueClient;
    private readonly QueueClient responseQueueClient;
    private readonly IConfig config;

    public InferenceWorkflowQueue(IConfig config)
    {
        requestQueueClient = new QueueClient(config.AzureQueueConnectionString, config.InferenceQueueName);
        responseQueueClient = new QueueClient(config.AzureQueueConnectionString, config.InferenceResponseQueueName);
        this.config = config;
    }
    public async Task<InferenceOutput> ExecuteAsync(string userInput, ChatHistory? chatHistory, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        await requestQueueClient.SendMessageAsync(JsonSerializer.Serialize(new InferenceRequestQueueMessage
        {
            CorrelationId = id,
            Query = userInput,
            ChatHistory = chatHistory?.Chats ?? [],
            ResponseQueueName = responseQueueClient.Name
        }), cancellationToken);

        while (true)
        {
            var message = await responseQueueClient.ReceiveMessageAsync(cancellationToken: cancellationToken);
            var r = JsonSerializer.Deserialize<InferenceResponseQueueMessage>(message.Value.Body);
            if (r is not null && r.CorrelationId == id)
            {
                await requestQueueClient.DeleteMessageAsync(
                    message.Value.MessageId, message.Value.PopReceipt, cancellationToken: cancellationToken);

                if (r.Output is null)
                {
                    throw new Exception("invalid response from inference workflow");
                }
                return r.Output;
            }

            await Task.Delay(TimeSpan.FromSeconds(config.IngestionReportEveryXSeconds), cancellationToken);
        }
    }
}
