using AIOChatbot.Llms;

namespace AIOChatbot;

public interface ILanguageModel
{
    Task<ChatCompletionResponse> GetChatCompletionsAsync(string text, LlmOptions options, ChatHistory? chatHistory = null);
}
