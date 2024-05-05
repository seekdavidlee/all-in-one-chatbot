namespace AIOChatbot.Llms;

public class ChatCompletionResponse
{
    public ChatCompletionResponse(string text)
    {
        this.Text = text;
    }
    public string Text { get; set; }
    public int? CompletionTokens { get; set; }
    public int? PromptTokens { get; set; }

}
