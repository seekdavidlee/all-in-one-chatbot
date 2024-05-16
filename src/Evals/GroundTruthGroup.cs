namespace AIOChatbot.Evals;

public class GroundTruthGroup
{
    public string? GroupId { get; set; }
    public List<GroundTruth> GroundTruths { get; set; } = [];
}