using chatbot2.Ingestions;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks.Dataflow;

namespace chatbot2.Commands;

public class IngestCommand : ICommandAction
{
    private readonly int concurrency;
    private readonly IEnumerable<IVectorDbIngestion> vectorDbIngestions;
    private readonly IVectorDb vectorDb;
    public IngestCommand(IEnumerable<IVectorDbIngestion> vectorDbIngestions, IEnumerable<IVectorDb> vectorDbs)
    {
        concurrency = int.Parse(Environment.GetEnvironmentVariable("Concurrency") ?? "2");
        this.vectorDbIngestions = vectorDbIngestions;
        vectorDb = vectorDbs.GetSelectedVectorDb();
    }
    public string Name => "ingest";

    public async Task ExecuteAsync(IConfiguration argsConfiguration)
    {
        await vectorDb.InitAsync();

        var sender = new ActionBlock<Func<Task>>((action) => action(),
        new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = concurrency,
            TaskScheduler = TaskScheduler.Default,
        });

        foreach (var ingestion in vectorDbIngestions)
        {
            await sender.SendAsync(() => ingestion.RunAsync(vectorDb));
        }
        sender.Complete();
        await sender.Completion;
    }
}
