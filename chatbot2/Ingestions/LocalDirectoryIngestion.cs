using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks.Dataflow;

namespace chatbot2.Ingestions;

public class LocalDirectoryIngestion : IVectorDbIngestion
{
    private readonly ILogger<LocalDirectoryIngestion> logger;

    public LocalDirectoryIngestion(ILogger<LocalDirectoryIngestion> logger)
    {
        this.logger = logger;
    }

    public async Task RunAsync(IVectorDb vectorDb, IEmbedding embedding)
    {
        var sender = new ActionBlock<Func<Task>>((action) => action(), Util.GetDataflowOptions());
        string[] dataSourcePaths = (Environment.GetEnvironmentVariable("DataSourcePaths") ?? throw new Exception("Missing DataSourcePaths!")).Split(',');
        var htmlReader = new HtmlReader();

        foreach (var dataSourcePath in dataSourcePaths)
        {
            logger.LogInformation("processing data source: {dataSourcePath}...", dataSourcePath);
            var (Pages, Logs) = await htmlReader.ReadFilesAsync(dataSourcePath);
            foreach (var page in Pages)
            {
                logger.LogInformation("processing page: {pagePath}...", page.Context.PagePath);
                foreach (var section in page.Sections)
                {
                    logger.LogInformation("processing section: {sectionPrefix}, {section}", section.IdPrefix, section);
                    await sender.SendAsync(() => ProcessAsync(vectorDb, embedding, section));
                }
            }
            foreach (var log in Logs)
            {
                logger.LogInformation($"log: {log.Text}, source: {log.Source}");
            }
        }

        sender.Complete();
        await sender.Completion;
    }

    private async Task ProcessAsync(IVectorDb vectorDb, IEmbedding embedding, PageSection pageSection)
    {
        try
        {
            var floatsList = await embedding.GetEmbeddingsAsync(pageSection.TextChunks.Select(x => x.Text ?? throw new Exception("text is null")).ToArray());
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
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "error processing page section");
        }
    }
}
