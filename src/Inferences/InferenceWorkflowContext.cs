using AIOChatbot.Llms;

namespace AIOChatbot.Inferences;

public class InferenceWorkflowContext
{
    public string? UserInput { get; set; }
    public ChatHistory? ChatHistory { get; set; }
    public Dictionary<string, Dictionary<string, object>> Steps { get; set; } = [];
}
