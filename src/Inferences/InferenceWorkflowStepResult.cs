namespace AIOChatbot.Inferences;

public class InferenceWorkflowStepResult(bool success, string? errorMessage = null)
{
    public bool Success { get; } = success;
    public string? ErrorMessage { get; } = errorMessage;
}
