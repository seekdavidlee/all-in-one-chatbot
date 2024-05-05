using chatbot2.Inferences;

namespace chatbot2.Models;

public class InferenceResponseQueueMessage
{
    public Guid CorrelationId { get; set; }

    public InferenceOutput? Output { get; set; }
}
