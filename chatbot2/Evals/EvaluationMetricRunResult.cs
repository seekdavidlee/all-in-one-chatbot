namespace chatbot2.Evals;

public class EvaluationMetricRunResult
{
    public int? Index { get; set; }
    public double DurationInMilliseconds { get; set; }
    public double? Score { get; set; }
    public string? ScoreReason { get; set; }
    public string? RawResponse { get; set; }
    public string? RawPrompt { get; set; }
    public bool? Success { get; set; }
    public string? ErrorMessage { get; set; }
}
