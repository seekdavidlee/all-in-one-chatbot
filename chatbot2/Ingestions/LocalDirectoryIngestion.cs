using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace chatbot2.Ingestions;

public class LocalDirectoryIngestion : IVectorDbIngestion
{
    private readonly ILogger<LocalDirectoryIngestion> logger;
    private readonly IEmbedding embedding;
    public LocalDirectoryIngestion(ILogger<LocalDirectoryIngestion> logger, IEnumerable<IEmbedding> embeddings)
    {
        this.logger = logger;
        embedding = embeddings.GetSelectedEmbedding();
    }
    public async Task RunAsync(IVectorDb vectorDb)
    {
        string[] dataSourcePaths = (Environment.GetEnvironmentVariable("DataSourcePaths") ?? throw new Exception("Missing DataSourcePaths!")).Split(',');
        var htmlReader = new HtmlReader();

        foreach (var dataSourcePath in dataSourcePaths)
        {
            logger.LogInformation("processing data source: {dataSourcePath}...", dataSourcePath);
            var result = await htmlReader.ReadFilesAsync(dataSourcePath);
            foreach (var page in result.Pages)
            {
                logger.LogInformation("processing page: {pagePath}...", page.Context.PagePath);
                foreach (var section in page.Sections)
                {
                    logger.LogInformation("processing section: {sectionPrefix}, {section}", section.IdPrefix, section);
                    await ProcessAsync(vectorDb, section);
                }
            }
            foreach (var log in result.Logs)
            {
                logger.LogInformation($"log: {log.Text}, source: {log.Source}");
            }
        }
    }

    private async Task ProcessAsync(IVectorDb vectorDb, PageSection pageSection)
    {
        List<SearchModel> models = [];
        foreach (var t in pageSection.TextChunks)
        {
            var m = new SearchModel
            {
                Id = Guid.NewGuid().ToString(),
                Content = t.Text,
                MetaData = JsonSerializer.Serialize(t.MetaDatas),
                Filepath = t.Id,
                ContentVector = await embedding.GetEmbeddingsAsync(t.Text ?? throw new Exception("Text cannot be null!")),
            };

            models.Add(m);
        }

        await vectorDb.ProcessAsync(models);
    }
}
