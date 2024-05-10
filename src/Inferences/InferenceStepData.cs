namespace AIOChatbot.Inferences;

public class InferenceStepData
{
    public InferenceStepData()
    {
        Outputs = [];
        Inputs = [];
    }

    public string? Name { get; set; }

    public Dictionary<string, string> Inputs { get; set; }
    public Dictionary<string, string> Outputs { get; set; }

    public int TryGetInputValue(string key, int defaultValue)
    {
        if (Inputs.TryGetValue(key, out string? value) && int.TryParse(value, out int result))
        {
            return result;
        }

        return defaultValue;
    }

    public void AddStepOutput(string name, string value)
    {
        Outputs[name] = value;
    }
}
