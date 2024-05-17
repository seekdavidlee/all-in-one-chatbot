namespace AIOChatbot.Evals;

public interface IGroundTruthReader
{
    string Name { get; }
    Task<IEnumerable<GroundTruthGroup>> ReadAsync(GroundTruthMapping groundTruthMapping);
}
