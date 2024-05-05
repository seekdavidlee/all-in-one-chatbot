using AIOChatbot.VectorDbs;

namespace AIOChatbot.Inferences;

public class InferenceOutput
{
    public string? Text { get; set; }
    public double? DurationInMilliseconds { get; set; }
    public IndexedDocument[]? Documents { get; set; }
    public int? CompletionTokens { get; set; }
    public int? PromptTokens { get; set; }

    public List<StepOutput>? Steps { get; set; }
}

public class StepOutput
{
    public StepOutput()
    {
        Items = [];
    }

    public string? Name { get; set; }

    public Dictionary<string, string> Items { get; set; }
}
