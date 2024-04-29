namespace chatbot2.Evals;

public class EvaluationConfig
{
    public string? Name { get; set; }
    public GroundTruthMapping[]? GroundTruthsMapping { get; set; }
    public EvaluationMetricConfig[]? Metrics { get; set; }
    public int? RunCount { get; set; }
    public string MachineName { get; set; } = Environment.MachineName;
}
