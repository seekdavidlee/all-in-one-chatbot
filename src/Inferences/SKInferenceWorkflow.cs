using AIOChatbot.Configurations;
using AIOChatbot.Llms;
using System.Diagnostics;

namespace AIOChatbot.Inferences;

public class SKInferenceWorkflow : IInferenceWorkflow
{
    private readonly IEnumerable<IInferenceWorkflowStep> inferenceWorkflowSteps;
    private readonly IConfig config;

    public SKInferenceWorkflow(IEnumerable<IInferenceWorkflowStep> inferenceWorkflowSteps, IConfig config)
    {
        this.inferenceWorkflowSteps = inferenceWorkflowSteps;
        this.config = config;
    }

    public async Task<InferenceOutput> ExecuteAsync(string userInput, ChatHistory? chatHistory, Dictionary<string, Dictionary<string, string>>? stepsInputs, CancellationToken cancellationToken)
    {
        var sw = new Stopwatch();
        sw.Start();

        var inferenceOutput = new InferenceOutput { Documents = [] };
        var context = new InferenceWorkflowContext(userInput, chatHistory);
        foreach (var stepName in config.InferenceWorkflowSteps)
        {
            if (stepName is null)
            {
                throw new Exception("missing stepName");
            }

            var step = inferenceWorkflowSteps.Single(x => x.GetType().Name == stepName);
            var data = new InferenceStepData(stepName);

            if (stepsInputs is not null && stepsInputs.TryGetValue(stepName, out var inputs))
            {
                data.Inputs = inputs;
            }

            context.StepsData.Add(data);

            var stepResult = await step.ExecuteAsync(context, cancellationToken);
            if (!stepResult.Success)
            {
                inferenceOutput.ErrorMessage = stepResult.ErrorMessage;
                inferenceOutput.ErroredStepName = stepName;
                break;
            }
            else
            {
                if (stepResult.Documents is not null)
                {
                    inferenceOutput.Documents = stepResult.Documents;
                }

                if (stepResult.Intents is not null)
                {
                    inferenceOutput.Intents = stepResult.Intents;
                }

                inferenceOutput.TotalCompletionTokens += stepResult.TotalCompletionTokens;
                inferenceOutput.TotalPromptTokens += stepResult.TotalPromptTokens;
                inferenceOutput.TotalEmbeddingTokens += stepResult.TotalEmbeddingTokens;
            }
        }

        sw.Stop();

        inferenceOutput.Text = context.BotResponse;
        inferenceOutput.Steps = context.StepsData;
        inferenceOutput.DurationInMilliseconds = sw.ElapsedMilliseconds;

        return inferenceOutput;
    }
}
