using AIOChatbot.Evals;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AIOChatbot.Commands;

public class EvaluationSummarizeCommand : ICommandAction
{
    private readonly EvaluationSummarizeWorkflow evaluationSummarizeWorkflow;
    private readonly ILogger<EvaluationSummarizeCommand> logger;

    public const string Command = "summarize";

    public EvaluationSummarizeCommand(EvaluationSummarizeWorkflow evaluationSummarizeWorkflow, ILogger<EvaluationSummarizeCommand> logger)
    {
        this.evaluationSummarizeWorkflow = evaluationSummarizeWorkflow;
        this.logger = logger;
    }
    public string Name => Command;
    public bool LongRunning => false;

    public async Task ExecuteAsync(IConfiguration argsConfiguration, CancellationToken cancellationToken)
    {
        var path = argsConfiguration["blob-path"];
        if (path is null)
        {
            logger.LogWarning("blob-path argument is missing");
            return;
        }

        await this.evaluationSummarizeWorkflow.CreateAsync(path);
    }
}
