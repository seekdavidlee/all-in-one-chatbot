using chatbot2.Evals;

namespace chatbot2.Models;

public class GroundTruthQueueMessage
{
    public string? GroudTruthStorageName { get; set; }
    public string? GroudTruthName { get; set; }
    public int? RunCount { get; set; }
    public string? ProjectPath { get; set; }
    public EvaluationMetricConfig? Metric { get; set; }
}
