using AIOChatbot.Configurations;
using AIOChatbot.Llms;

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

    public async Task<InferenceOutput> ExecuteAsync(string userInput, ChatHistory? chatHistory, CancellationToken cancellationToken)
    {
        var inferenceOutput = new InferenceOutput();
        var context = new InferenceWorkflowContext(userInput, chatHistory);
        foreach (var stepName in config.InferenceWorkflowSteps)
        {
            if (stepName is null)
            {
                throw new Exception("missing stepName");
            }

            var step = inferenceWorkflowSteps.Single(x => x.GetType().Name == stepName);
            context.StepsData.Add(new InferenceStepData(step.GetType().Name));

            // todo: add step inputs

            var stepResult = await step.ExecuteAsync(context, cancellationToken);
            if (!stepResult.Success)
            {
                inferenceOutput.ErrorMessage = stepResult.ErrorMessage;
                inferenceOutput.ErroredStepName = stepName;
                break;
            }
        }

        return new InferenceOutput
        {
            Text = context.BotResponse,
            Steps = context.StepsData
        };
    }
}
