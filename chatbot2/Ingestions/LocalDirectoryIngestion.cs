using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks.Dataflow;

namespace chatbot2.Ingestions;

public class LocalDirectoryIngestion : IVectorDbIngestion
{
    private readonly IngestionReporter ingestionReporter;
    private readonly ILogger<LocalDirectoryIngestion> logger;

    public LocalDirectoryIngestion(IngestionReporter ingestionReporter, ILogger<LocalDirectoryIngestion> logger)
    {
        this.ingestionReporter = ingestionReporter;
        this.logger = logger;
    }

    public async Task RunAsync(IVectorDb vectorDb, IEmbedding embedding, CancellationToken cancellationToken)
    {
        var sender = new ActionBlock<Func<Task>>((action) => action(), Util.GetDataflowOptions(cancellationToken));
        string[] dataSourcePaths = (Environment.GetEnvironmentVariable("DataSourcePaths") ?? throw new Exception("Missing DataSourcePaths!")).Split(',');
        var htmlReader = new HtmlReader();

        foreach (var dataSourcePath in dataSourcePaths)
        {
            logger.LogDebug("processing data source: {dataSourcePath}...", dataSourcePath);
            var (Pages, Logs) = await htmlReader.ReadFilesAsync(dataSourcePath, cancellationToken);
            foreach (var page in Pages)
            {
                logger.LogDebug("processing page: {pagePath}...", page.Context.PagePath);
                foreach (var section in page.Sections)
                {
                    logger.LogDebug("processing section: {sectionPrefix}, {section}", section.IdPrefix, section);
                    await sender.SendAsync(() => ProcessAsync(vectorDb, embedding, section, cancellationToken));
                }
            }
            foreach (var log in Logs)
            {
                logger.LogInformation("log: {logText}, source: {logSource}", log.Text, log.Source);
            }
        }

        sender.Complete();
        await sender.Completion;
    }

    private async Task ProcessAsync(IVectorDb vectorDb, IEmbedding embedding, PageSection pageSection, CancellationToken cancellationToken)
    {
        try
        {
            this.ingestionReporter.IncrementSearchModelsProcessing(pageSection.TextChunks.Count());
            var floatsList = await embedding.GetEmbeddingsAsync(pageSection.TextChunks.Select(
                x => x.Text ?? throw new Exception("text is null")).ToArray(), cancellationToken);

            List<SearchModel> models = [];
            for (int i = 0; i < pageSection.TextChunks.Count; i++)
            {
                var t = pageSection.TextChunks[i];
                var m = new SearchModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = t.Text,
                    MetaData = JsonSerializer.Serialize(t.MetaDatas),
                    Filepath = t.Id,
                    ContentVector = floatsList[i]
                };

                models.Add(m);
            }

            await vectorDb.ProcessAsync(models);
            this.ingestionReporter.IncrementSearchModelsProcessed(models.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "error processing page section");
        }
    }
}
