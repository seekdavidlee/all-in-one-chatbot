namespace AIOChatbot.Inferences;

public interface IInferenceWorkflowStep
{
    Task<bool> ExecuteAsync(InferenceWorkflowContext context, CancellationToken cancellationToken);
}
