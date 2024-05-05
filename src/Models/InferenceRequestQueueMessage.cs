using AIOChatbot.Llms;

namespace AIOChatbot.Models;

public class InferenceRequestQueueMessage
{
    public Guid CorrelationId { get; set; }
    public string? Query { get; set; }
    public List<ChatEntry>? ChatHistory { get; set; }
    public string? ResponseQueueName { get; set; }
}
