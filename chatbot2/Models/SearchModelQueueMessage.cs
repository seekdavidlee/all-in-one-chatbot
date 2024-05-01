namespace chatbot2;

public class SearchModelQueueMessage
{
    public Guid Id { get; set; }

    public string? CollectionName { get; set; }

    public string? EmbeddingType { get; set; }
}
