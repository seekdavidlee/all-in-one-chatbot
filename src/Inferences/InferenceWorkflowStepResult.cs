using AIOChatbot.VectorDbs;

namespace AIOChatbot.Inferences;

public class InferenceWorkflowStepResult(bool success, string? errorMessage = null)
{
    public bool Success { get; } = success;
    public string? ErrorMessage { get; } = errorMessage;
    public IndexedDocument[]? Documents { get; set; }
    public int TotalCompletionTokens { get; set; }
    public int TotalPromptTokens { get; set; }
    public int TotalEmbeddingTokens { get; set; }
    public string[]? Intents { get; set; }
}
