using AIOChatbot.Configurations;
using AIOChatbot.Llms;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AIOChatbot.Inferences;

public class SKInferenceWorkflow : IInferenceWorkflow
{
    private readonly IEnumerable<IInferenceWorkflowStep> inferenceWorkflowSteps;
    private readonly IConfig config;
    private readonly ILogger<SKInferenceWorkflow> logger;
    const string StepDurationInMillisecondsKey = "StepDurationInMilliseconds";

    public SKInferenceWorkflow(IEnumerable<IInferenceWorkflowStep> inferenceWorkflowSteps, IConfig config, ILogger<SKInferenceWorkflow> logger)
    {
        this.inferenceWorkflowSteps = inferenceWorkflowSteps;
        this.config = config;
        this.logger = logger;
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

            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogDebug("step: {stepName} is cancelled", stepName);
                break;
            }

            logger.LogDebug("executing step: {stepName}", stepName);

            var step = inferenceWorkflowSteps.Single(x => x.GetType().Name == stepName);
            var data = new InferenceStepData(stepName);

            if (stepsInputs is not null && stepsInputs.TryGetValue(stepName, out var inputs))
            {
                data.Inputs = inputs;
                logger.LogDebug("executing step: {stepName} with inputs", stepName);
            }

            context.StepsData.Add(data);

            var stepResultSw = new Stopwatch();
            stepResultSw.Start();
            InferenceWorkflowStepResult stepResult;
            try
            {
                stepResult = await step.ExecuteAsync(context, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogDebug("step: {stepName} is cancelled", stepName);
                    break;
                }
            }
            catch (Exception ex)
            {
                var userCorrelationId = Guid.NewGuid();
                inferenceOutput.IsInternalError = true;
                inferenceOutput.ErrorMessage = $"An internal error has occured. Please use this correlation Id: {userCorrelationId} to track this error.";
                inferenceOutput.ErroredStepName = stepName;

                logger.LogError(ex, "error executing step: {stepName}, userCorrelationId: {userCorrelationId}", stepName, userCorrelationId);
                break;
            }
            finally
            {
                stepResultSw.Stop();
                var currentStepData = context.GetStepData(stepName);
                currentStepData.AddStepOutput(StepDurationInMillisecondsKey, stepResultSw.ElapsedMilliseconds);
            }

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
