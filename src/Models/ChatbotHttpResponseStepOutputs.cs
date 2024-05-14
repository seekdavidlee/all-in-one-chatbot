using AIOChatbot.Inferences;

namespace AIOChatbot.Models;

public class ChatbotHttpResponseStepOutputs
{
    public double? InferenceDurationInMilliseconds { get; set; }
    public Dictionary<string, Dictionary<string, object>>? StepOutputs { get; set; }
}

public static class ChatbotHttpResponseMetricsExtensions
{
    public static ChatbotHttpResponseStepOutputs ToChatbotHttpResponseMetrics(this InferenceOutput inferenceOutput)
    {
        var chatbotHttpResponseMetrics = new ChatbotHttpResponseStepOutputs
        {
            InferenceDurationInMilliseconds = inferenceOutput.DurationInMilliseconds,
        };

        if (inferenceOutput.Steps is not null)
        {
            chatbotHttpResponseMetrics.StepOutputs = inferenceOutput.Steps.ToDictionary(
                stepData => stepData.Name ?? throw new Exception("step name is missing"), stepData => stepData.Outputs);
        }

        return chatbotHttpResponseMetrics;
    }
}
