namespace AIOChatbot.Inferences;

public interface IInferenceWorkflowStep
{
    Task<InferenceWorkflowStepResult> ExecuteAsync(InferenceWorkflowContext context, CancellationToken cancellationToken);

    Dictionary<string, string> CreateInputs();
}
