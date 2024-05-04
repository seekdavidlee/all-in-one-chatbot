using chatbot2.Llms;

namespace chatbot2.Inferences;

public interface IInferenceWorkflow
{
    Task<InferenceOutput> ExecuteAsync(string userInput, ChatHistory? chatHistory, CancellationToken cancellationToken);
}
