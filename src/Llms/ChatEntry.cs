namespace AIOChatbot.Llms;

public class ChatEntry
{
    public string? User { get; set; }
    public string? Bot { get; set; }
    public string[]? Intents { get; set; }
    public int? UserPromptTokens { get; set; }
    public int? BotCompletionTokens { get; set; }
    public int? BotEmbeddingTokens { get; set; }
    public const string IntentsKey = "intents";
}

