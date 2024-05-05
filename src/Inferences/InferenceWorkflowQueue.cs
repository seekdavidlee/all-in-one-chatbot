using Azure.Storage.Queues;
using AIOChatbot.Configuration;
using AIOChatbot.Llms;
using AIOChatbot.Models;
using System.Text.Json;

namespace AIOChatbot.Inferences;

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
            if (message is not null)
            {
                var response = JsonSerializer.Deserialize<InferenceResponseQueueMessage>(message.Value.Body);
                if (response is not null && response.CorrelationId == id)
                {
                    await responseQueueClient.DeleteMessageAsync(
                        message.Value.MessageId, message.Value.PopReceipt, cancellationToken: cancellationToken);

                    if (response.Output is null)
                    {
                        throw new Exception("invalid response from inference workflow");
                    }
                    return response.Output;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(config.IngestionQueuePollingInterval), cancellationToken);
        }
    }
}
