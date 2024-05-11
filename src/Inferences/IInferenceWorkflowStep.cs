namespace AIOChatbot.Inferences;

public interface IInferenceWorkflowStep
{
    Task<InferenceWorkflowStepResult> ExecuteAsync(InferenceWorkflowContext context, CancellationToken cancellationToken);
}
