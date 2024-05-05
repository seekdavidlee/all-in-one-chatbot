using System.Text.Json;

namespace AIOChatbot.Evals;

public class EvaluationMetricResult
{
    public string? MetricName { get; set; }
    public GroundTruth? GroundTruth { get; set; }
    public EvaluationMetricRunResult[]? Results { get; set; }
    public double? DurationInMilliseconds { get; set; }
    public int? GroundTruthCompletionTokens { get; set; }
    public int? GroundTruthPromptTokens { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }
}
