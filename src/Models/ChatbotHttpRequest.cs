using AIOChatbot.Llms;

namespace AIOChatbot.Models;

public class ChatbotHttpRequest
{
    public string? Query { get; set; }
    public List<ChatEntry>? ChatHistory { get; set; }
    public Dictionary<string, Dictionary<string, string>>? StepsInputs { get; set; }
}
