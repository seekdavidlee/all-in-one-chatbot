using LLama.Common;
using LLama;
using System.Text;

namespace chatbot2.Llms;

public class LocalLLM : ILanguageModel
{
    private readonly LLamaContext context;
    public LocalLLM()
    {
        string modelPath = Environment.GetEnvironmentVariable("ModelFilePath") ?? throw new Exception("Missing ModelFilePath!"); // change it to your own model path.

        var parameters = new ModelParams(modelPath)
        {
            ContextSize = 1024, // The longest length of chat as memory.
            GpuLayerCount = 5, // How many layers to offload to GPU. Please adjust it according to your GPU memory.
        };
        using var model = LLamaWeights.LoadFromFile(parameters);
        context = model.CreateContext(parameters);
    }

    public async Task<string> GetChatCompletionsAsync(string text, LlmOptions options)
    {
        InferenceParams inferenceParams = new()
        {
            MaxTokens = 256, // No more than 256 tokens should appear in answer. Remove it if antiprompt is enough for control.
            AntiPrompts = ["User:"], // Stop generation once antiprompts appear.
            Temperature = 0,
            //TopK = 10,
            //TopP = 0.1F,
        };

        StringBuilder sb = new();
        var instructExecutor = new InstructExecutor(context);
        var answers = instructExecutor.InferAsync(text, inferenceParams);
        await foreach (var answer in answers)
        {
            sb.Append(answer);
        }

        return sb.ToString();
    }
}
