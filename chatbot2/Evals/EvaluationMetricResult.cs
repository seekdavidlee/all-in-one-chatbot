namespace chatbot2.Evals;

public class EvaluationMetricResult
{
    public string? MetricName { get; set; }
    public GroundTruth? GroundTruth { get; set; }
    public EvaluationMetricRunResult[]? Results { get; set; }
    public double? DurationInMilliseconds { get; set; }
    public int? GroundTruthCompletionTokens { get; set; }
    public int? GroundTruthPromptTokens { get; set; }
}
