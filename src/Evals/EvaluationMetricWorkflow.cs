using AIOChatbot.Inferences;
using AIOChatbot.Llms;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AIOChatbot.Evals;

public class EvaluationMetricWorkflow
{
    private readonly IEnumerable<ILanguageModel> languageModels;
    private readonly FileCache fileCache;
    private readonly ILogger<EvaluationMetricWorkflow> logger;

    public EvaluationMetricWorkflow(IEnumerable<ILanguageModel> languageModels, FileCache fileCache, ILogger<EvaluationMetricWorkflow> logger)
    {
        this.languageModels = languageModels;
        this.fileCache = fileCache;
        this.logger = logger;
    }

    public async Task<EvaluationMetricResult?> RunAsync(EvaluationMetricConfig config, GroundTruth groundTruth, InferenceOutput output, CancellationToken cancellationToken)
    {
        if (config.PromptFilePath is null)
        {
            logger.LogDebug("prompt file path is invalid");
            return default;
        }

        var llm = languageModels.FirstOrDefault(x => x.GetType().Name == config.LanguageModelType);
        if (llm is null)
        {
            logger.LogDebug("language model {languageModel} not found", config.LanguageModelType);
            return default;
        }

        Stopwatch outerSw = new();
        outerSw.Start();
        var result = new EvaluationMetricResult
        {
            MetricName = config.Name,
            GroundTruth = groundTruth,
        };

        var prompt = await fileCache.GetFileContentAsync(config.PromptFilePath, cancellationToken);
        List<EvaluationMetricRunResult> results = [];
        for (int i = 0; i < config.RunCount; i++)
        {
            var metricResult = new EvaluationMetricRunResult
            {
                Index = i,
            };

            prompt = prompt.Replace("{{chat_history_text}}", groundTruth.ChatHistory is not null ? groundTruth.ChatHistory.FullBody() : "");
            prompt = prompt.Replace("{{question}}", groundTruth.Question);
            prompt = prompt.Replace("{{answer}}", output.Text);
            metricResult.RawPrompt = prompt.Replace("{{documents}}", output.Documents?.FullBody());

            Stopwatch sw = new();
            sw.Start();
            try
            {
                var llmResult = await llm.GetChatCompletionsAsync(metricResult.RawPrompt, new LlmOptions
                {
                    DeploymentName = config.DeploymentName,
                    MaxTokens = config.MaxTokens,
                    Temperature = config.Temperature,
                });
                metricResult.RawResponse = llmResult.Text;
                metricResult.Success = true;
                metricResult.CompletionTokens = llmResult.CompletionTokens;
                metricResult.PromptTokens = llmResult.PromptTokens;
                metricResult.InferencePromptTokens = output.PromptTokens;
                metricResult.InferenceCompletionTokens = output.CompletionTokens;
            }
            catch (Exception ex)
            {
                metricResult.Success = false;
                metricResult.ErrorMessage = ex.Message;
            }
            finally
            {
                sw.Stop();
            }

            metricResult.DurationInMilliseconds = sw.ElapsedMilliseconds;

            if (config.ValueStartIndexOf is not null &&
                config.ValueEndIndexOf is not null &&
                metricResult.RawResponse is not null)
            {
                if (double.TryParse(GetString(config.ValueStartIndexOf, config.ValueEndIndexOf, metricResult.RawResponse), out double val))
                {
                    metricResult.Score = val;
                }
            }

            if (config.ReasoningStartIndexOf is not null &&
                config.ReasoningEndIndexOf is not null &&
                metricResult.RawResponse is not null)
            {
                metricResult.ScoreReason = GetString(config.ReasoningStartIndexOf, config.ReasoningEndIndexOf, metricResult.RawResponse);
            }

            results.Add(metricResult);
        }

        result.Results = [.. results];

        outerSw.Stop();
        result.DurationInMilliseconds = outerSw.ElapsedMilliseconds;
        return result;
    }

    private static string? GetString(string startIndex, string endIndex, string text)
    {
        var start = text.IndexOf(startIndex, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return default;
        }

        var end = text.IndexOf(endIndex, start, StringComparison.OrdinalIgnoreCase);
        if (end < 0)
        {
            return default;
        }

        return text.Substring(start + startIndex.Length, end - start - startIndex.Length);
    }
}
