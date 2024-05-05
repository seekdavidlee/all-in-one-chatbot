namespace AIOChatbot.Evals;

public class GroundTruth
{
    public string? Question { get; set; }
    public string? Answer { get; set; }
    public string? DataSource { get; set; }
    public string? EntrySource { get; set; }
    public IDictionary<string, object>? Metadata { get; set; }
}