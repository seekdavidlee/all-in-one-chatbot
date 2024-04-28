using chatbot2.Ingestions;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks.Dataflow;

namespace chatbot2.Commands;

public class IngestCommand : ICommandAction
{
    private readonly IEnumerable<IVectorDbIngestion> vectorDbIngestions;
    private readonly IVectorDb vectorDb;
    private readonly IEmbedding embedding;
    public IngestCommand(IEnumerable<IVectorDbIngestion> vectorDbIngestions, IEnumerable<IVectorDb> vectorDbs, IEnumerable<IEmbedding> embeddings)
    {
        this.vectorDbIngestions = vectorDbIngestions;
        vectorDb = vectorDbs.GetSelectedVectorDb();
        embedding = embeddings.GetSelectedEmbedding();
    }
    public string Name => "ingest";

    public async Task ExecuteAsync(IConfiguration argsConfiguration)
    {
        await vectorDb.InitAsync();

        var sender = new ActionBlock<Func<Task>>((action) => action(), Util.GetDataflowOptions(vectorDbIngestions.Count()));

        foreach (var ingestion in vectorDbIngestions)
        {
            await sender.SendAsync(() => ingestion.RunAsync(vectorDb, embedding));
        }
        sender.Complete();
        await sender.Completion;
    }
}
