using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using chatbot2.Configuration;
using System.Text;
using System.Text.Json;

namespace chatbot2.Ingestions;

public class IngestionQueueService : IIngestionProcessor
{
    private readonly QueueClient queueClient;
    private readonly IConfig config;
    private static readonly Guid jobId = Guid.NewGuid();
    public IngestionQueueService(IConfig config)
    {
        queueClient = new(config.AzureQueueConnectionString, config.IngestionQueueName);
        this.config = config;
    }

    public async Task ProcessAsync(List<SearchModelDto> searchModels, string collectionName, CancellationToken cancellationToken)
    {
        // send defined embedding type and collectionName (search index) to the queue
        var msg = new SearchModelQueueMessage
        {
            Id = Guid.NewGuid(),
            CollectionName = collectionName,
            EmbeddingType = config.EmbeddingType,
            JobId = jobId
        };
        var blob = new BlobClient(config.AzureStorageConnectionString, config.IngestionQueueStorageName, $"{jobId}\\{msg.Id}");
        await blob.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(searchModels))), cancellationToken);
        await queueClient.SendMessageAsync(JsonSerializer.Serialize(msg), cancellationToken);
    }
}
