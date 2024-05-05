using AIOChatbot.Llms;

namespace AIOChatbot.Inferences;

public interface IInferenceWorkflow
{
    Task<InferenceOutput> ExecuteAsync(string userInput, ChatHistory? chatHistory, CancellationToken cancellationToken);
}
