using AIOChatbot.Evals;

namespace AIOChatbot.Models;

public class GroundTruthQueueMessage
{
    public string? GroudTruthStorageName { get; set; }
    public string? GroudTruthName { get; set; }
    public int? RunCount { get; set; }
    public string? ProjectPath { get; set; }
    public EvaluationMetricConfig? Metric { get; set; }
}
