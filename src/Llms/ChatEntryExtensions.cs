using AIOChatbot.Inferences;

namespace AIOChatbot.Llms;

public static class ChatEntryExtensions
{
    public static string[] GetIntents(this InferenceOutput inferenceOutput)
    {
        List<string> intents = [];

        if (inferenceOutput.Steps is not null)
        {
            foreach (var step in inferenceOutput.Steps)
            {
                if (step.Items.TryGetValue(ChatEntry.IntentsKey, out string? intentsText) && intentsText is not null)
                {
                    intents.AddRange(intentsText.Split(','));
                }
            }
        }

        return [.. intents];
    }
}

