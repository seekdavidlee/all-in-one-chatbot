using chatbot2.Ingestions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;

namespace chatbot2.Commands;

public class IngestCommand : ICommandAction
{
    private readonly IEnumerable<IVectorDbIngestion> vectorDbIngestions;
    private readonly IngestionReporter ingestionReporter;
    private readonly ILogger<IngestCommand> logger;
    private readonly IVectorDb vectorDb;
    private readonly IEmbedding embedding;
    private readonly int reportEveryXSeconds = 10;
    public IngestCommand(IEnumerable<IVectorDbIngestion> vectorDbIngestions,
        IEnumerable<IVectorDb> vectorDbs,
        IEnumerable<IEmbedding> embeddings,
        IngestionReporter ingestionReporter,
        ILogger<IngestCommand> logger)
    {
        this.vectorDbIngestions = vectorDbIngestions;
        this.ingestionReporter = ingestionReporter;
        this.logger = logger;
        vectorDb = vectorDbs.GetSelectedVectorDb();
        embedding = embeddings.GetSelectedEmbedding();

        var reportEveryXSecondsStr = Environment.GetEnvironmentVariable("IngestionReportEveryXSeconds");
        if (reportEveryXSecondsStr is not null)
        {
            if (int.TryParse(reportEveryXSecondsStr, out int reportEveryXSecondsInt))
            {
                reportEveryXSeconds = reportEveryXSecondsInt;
            }
        }

        if (reportEveryXSeconds < 1)
        {
            throw new Exception($"reportEveryXSeconds value of {reportEveryXSeconds} is invalid!");
        }
    }
    public string Name => "ingest";

    public async Task ExecuteAsync(IConfiguration argsConfiguration, CancellationToken cancellationToken)
    {
        await vectorDb.InitAsync();

        var sender = new ActionBlock<Func<Task>>((action) => action(), Util.GetDataflowOptions(cancellationToken, vectorDbIngestions.Count()));

        var started = DateTime.UtcNow;
        var timer = new Timer((o) => this.ingestionReporter.Report(),
            null, TimeSpan.FromSeconds(reportEveryXSeconds), TimeSpan.FromSeconds(reportEveryXSeconds));

        int doneCount = vectorDbIngestions.Count();
        foreach (var ingestion in vectorDbIngestions)
        {
            logger.LogInformation("queuing ingestion: {ingestion}", ingestion.GetType().Name);
            await sender.SendAsync(() => ingestion.RunAsync(vectorDb, embedding, cancellationToken));
        }
        sender.Complete();
        await sender.Completion;
        timer.Dispose();
    }
}
