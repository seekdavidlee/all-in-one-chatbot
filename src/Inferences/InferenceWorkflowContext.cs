using AIOChatbot.Llms;

namespace AIOChatbot.Inferences;

public class InferenceWorkflowContext
{
    public InferenceWorkflowContext(string userInput, ChatHistory? chatHistory)
    {
        UserInput = userInput;
        ChatHistory = chatHistory;
    }
    public string UserInput { get; }
    public ChatHistory? ChatHistory { get; }
    public List<InferenceStepData> StepsData { get; set; } = [];

    public string? BotResponse { get; set; }

    public InferenceStepData GetStepData(string name)
    {
        return StepsData.Single(s => s.Name == name);
    }
}
