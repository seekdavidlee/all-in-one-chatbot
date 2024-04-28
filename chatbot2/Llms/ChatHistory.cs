using System.Text;

namespace chatbot2.Llms;

public class ChatHistory
{
    public List<ChatEntry>? Chats { get; set; }
    public int? MaxTokens { get; set; }

    const string SPEAKER_KEY = "speaker: user";
    const string ASSISTANT_KEY = "speaker: assistant";
    public string FullBody()
    {
        if (Chats is null)
        {
            throw new Exception("Chats is not initialized!");
        }

        // todo: trim chat history if too long

        StringBuilder sb = new();
        for (var i = Chats.Count - 1; i > -1; i--)
        {
            var entry = Chats[i];
            sb.AppendLine(SPEAKER_KEY);
            sb.AppendLine($"message: {entry.User}");
            sb.AppendLine(ASSISTANT_KEY);
            sb.AppendLine($"message: {entry.Bot}");
        }

        return sb.ToString();
    }
}
