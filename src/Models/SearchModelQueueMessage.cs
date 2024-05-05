namespace AIOChatbot;

public class SearchModelQueueMessage
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }

    public string? CollectionName { get; set; }

    public string? EmbeddingType { get; set; }
}
