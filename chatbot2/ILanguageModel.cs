using chatbot2.Llms;

namespace chatbot2;

public interface ILanguageModel
{
    Task<string> GetChatCompletionsAsync(string text, LlmOptions options);
}
