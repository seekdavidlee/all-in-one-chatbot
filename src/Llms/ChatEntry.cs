namespace AIOChatbot.Llms;

public class ChatEntry
{
    public string? User { get; set; }
    public string? Bot { get; set; }
    public string[]? Intents { get; set; }
    public int? UserTokens { get; set; }
    public int? BotTokens { get; set; }
    public const string IntentsKey = "intents";
}

