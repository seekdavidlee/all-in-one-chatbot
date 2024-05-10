using AIOChatbot.Llms;

namespace AIOChatbot.Inferences;

public class InferenceWorkflowContext
{
    public string? UserInput { get; set; }
    public ChatHistory? ChatHistory { get; set; }
    public List<InferenceStepData> StepsData { get; set; } = [];

    public InferenceStepData GetStepData(string name)
    {
        return StepsData.Single(s => s.Name == name);
    }
}
