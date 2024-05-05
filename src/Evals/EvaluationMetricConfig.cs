namespace AIOChatbot.Evals;

public class EvaluationMetricConfig
{
    public string? Name { get; set; }
    public string? VectorDbType { get; set; }
    public string? EmbeddingType { get; set; }
    public string? LanguageModelType { get; set; }
    public string? CollectionName { get; set; }
    public int? RunCount { get; set; }
    public string? PromptFilePath { get; set; }
    public string? ValueStartIndexOf { get; set; }
    public string? ReasoningStartIndexOf { get; set; }
    public string? ValueEndIndexOf { get; set; }
    public string? ReasoningEndIndexOf { get; set; }
    public string? DeploymentName { get; set; }
    public int? MaxTokens { get; set; }
    public float? Temperature { get; set; }
    // todo: top_k, top_p, etc
}