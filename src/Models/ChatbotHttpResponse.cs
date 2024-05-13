using AIOChatbot.VectorDbs;

namespace AIOChatbot.Models;

public class ChatbotHttpResponse : ChatbotHttpResponseStepOutputs
{
    public string? Bot { get; set; }

    public string[]? Intents { get; set; }
    public int TotalCompletionTokens { get; set; }
    public int TotalPromptTokens { get; set; }
    public int TotalEmbeddingTokens { get; set; }
    public List<ChatbotDocumentHttpResponse>? Documents { get; set; }
}
