using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using chatbot2.Configuration;
using System.Text;
using System.Text.Json;

namespace chatbot2.Ingestions;

public class QueueService : IIngestionProcessor
{
    private readonly QueueClient queueClient;
    private readonly IConfig config;
    public QueueService(IConfig config)
    {
        queueClient = new(config.AzureQueueConnectionString, config.IngestionQueueName);
        this.config = config;
    }

    public async Task ProcessAsync(List<SearchModelDto> searchModels, CancellationToken cancellationToken)
    {
        // send defined embedding type and collectionName (search index) to the queue
        var msg = new SearchModelQueueMessage
        {
            Id = Guid.NewGuid(),
            CollectionName = config.CollectionName,
            EmbeddingType = config.EmbeddingType,
        };
        var blob = new BlobClient(config.AzureStorageConnectionString, config.IngestionQueueStorageName, msg.Id.ToString());
        await blob.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(searchModels))), cancellationToken);
        await queueClient.SendMessageAsync(JsonSerializer.Serialize(msg), cancellationToken);
    }
}
