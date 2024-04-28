using chatbot2.Evals;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace chatbot2.Commands;

public class ShowEvaluationMetricResultCommand : ICommandAction
{
    private readonly ReportRepository reportRepository;
    private readonly ILogger<ShowEvaluationMetricResultCommand> logger;

    public ShowEvaluationMetricResultCommand(ReportRepository reportRepository, ILogger<ShowEvaluationMetricResultCommand> logger)
    {
        this.reportRepository = reportRepository;
        this.logger = logger;
    }
    public string Name => "show-metric-eval";

    public async Task ExecuteAsync(IConfiguration argsConfiguration)
    {
        var path = argsConfiguration["blob-path"];
        if (path is null)
        {
            logger.LogWarning("blob-path argument is missing");
            return;
        }

        var item = await reportRepository.GetItemAsync<EvaluationMetricResult>(path);
        if (item is null)
        {
            logger.LogWarning("No evaluation metric result found at {path}", path);
            return;
        }

        Console.WriteLine(item);
    }
}
