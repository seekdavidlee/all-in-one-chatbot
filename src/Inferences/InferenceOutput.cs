using AIOChatbot.VectorDbs;

namespace AIOChatbot.Inferences;

public class InferenceOutput
{
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the error message if there was an error during inference.
    /// </summary>
    public string? ErrorMessage { get; set; }

    public bool? IsInternalError { get; set; }

    /// <summary>
    /// Gets or sets the step where the error occurred.
    /// </summary>
    public string? ErroredStepName { get; set; }

    public double? DurationInMilliseconds { get; set; }
    public IndexedDocument[]? Documents { get; set; }
    public int TotalCompletionTokens { get; set; }
    public int TotalPromptTokens { get; set; }
    public int TotalEmbeddingTokens { get; set; }
    public string[]? Intents { get; set; }
    public List<InferenceStepData>? Steps { get; set; }
}
