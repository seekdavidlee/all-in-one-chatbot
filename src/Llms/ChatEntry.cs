namespace chatbot2.Llms;

public class ChatEntry
{
    public string? User { get; set; }
    public string? Bot { get; set; }
    public int? UserTokens { get; set; }
    public int? BotTokens { get; set; }
}

