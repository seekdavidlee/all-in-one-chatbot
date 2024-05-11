using AIOChatbot.Llms;

namespace AIOChatbot.Models;

public class ChatbotHttpRequest
{
    public string? Query { get; set; }
    public List<ChatEntry>? ChatHistory { get; set; }
}
