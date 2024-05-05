namespace AIOChatbot.Evals;

public interface IGroundTruthReader
{
    string Name { get; }
    Task<IEnumerable<GroundTruth>> ReadAsync(GroundTruthMapping groundTruthMapping);
}
