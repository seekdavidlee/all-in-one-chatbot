using AIOChatbot.Inferences;

namespace AIOChatbot.Models;

public class InferenceResponseQueueMessage
{
    public Guid CorrelationId { get; set; }

    public InferenceOutput? Output { get; set; }
}
