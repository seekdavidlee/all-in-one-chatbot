using chatbot2.Llms;

namespace chatbot2;

public interface ILanguageModel
{
    Task<ChatCompletionResponse> GetChatCompletionsAsync(string text, LlmOptions options, ChatHistory? chatHistory = null);
}
