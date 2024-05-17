namespace AIOChatbot.Evals;

public class GroundTruth
{
    public string? Question { get; set; }
    public string? Answer { get; set; }
    public string? DataSource { get; set; }
    public string? EntrySource { get; set; }
    public string? Intent { get; set; }
    public IDictionary<string, object>? Metadata { get; set; }
    public List<GroundTruthCitation>? Citations { get; set; }
    public List<GroundTruth> PreviousGroundTruths { get; set; } = [];
}
