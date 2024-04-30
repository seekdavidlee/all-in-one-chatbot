using chatbot2.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks.Dataflow;

namespace chatbot2.Ingestions;

public class LocalDirectoryIngestion : IVectorDbIngestion
{
    private readonly IngestionReporter ingestionReporter;
    private readonly IConfig config;
    private readonly ILogger<LocalDirectoryIngestion> logger;
    private readonly int batchSize = 30;

    public LocalDirectoryIngestion(IngestionReporter ingestionReporter, IConfig config, ILogger<LocalDirectoryIngestion> logger)
    {
        this.ingestionReporter = ingestionReporter;
        this.config = config;
        this.logger = logger;

        var ingestionBatchSize = Environment.GetEnvironmentVariable("IngestionBatchSize");
        if (ingestionBatchSize is not null)
        {
            if (int.TryParse(ingestionBatchSize, out int batchSize))
            {
                this.batchSize = batchSize;
            }
        }
    }

    public async Task RunAsync(IVectorDb vectorDb, IEmbedding embedding, CancellationToken cancellationToken)
    {
        var sender = new ActionBlock<Func<Task>>((action) => action(), Util.GetDataflowOptions(cancellationToken));
        var dataSourcePathsStr = (Environment.GetEnvironmentVariable("DataSourcePaths") ?? throw new Exception("Missing DataSourcePaths"));
        bool isBlob = dataSourcePathsStr.StartsWith(Util.BlobPrefix);
        string[] dataSourcePaths = (isBlob ? dataSourcePathsStr[Util.BlobPrefix.Length..] : dataSourcePathsStr).Split(',');
        var htmlReader = new HtmlReader(this.config, this.logger);
        int totalRecords = 0;
        foreach (var dataSourcePath in dataSourcePaths)
        {
            logger.LogInformation("processing data source: {dataSourcePath}...", dataSourcePath);
            var (Pages, Logs) = isBlob ? await htmlReader.ReadBlobsAsync(dataSourcePath, cancellationToken) : await htmlReader.ReadFilesAsync(dataSourcePath, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            int size = 0;
            List<TextChunk> chunks = new();
            foreach (var page in Pages)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                if (page.Sections.Count == 0)
                {
                    logger.LogWarning("page has no sections: {pagePath}", page.Context.PagePath);
                    continue;
                }

                logger.LogDebug("processing page: {pagePath}...", page.Context.PagePath);

                totalRecords += page.Sections.Sum(x => x.TextChunks.Count);

                foreach (var section in page.Sections)
                {
                    foreach (var txtChunk in section.TextChunks)
                    {
                        if (size + txtChunk.TokenCount > this.batchSize)
                        {
                            await sender.SendAsync(() => ProcessAsync(vectorDb, embedding, chunks.ToArray(), cancellationToken));
                            size = 0;
                            chunks.Clear();
                        }
                        size += txtChunk.TokenCount;
                        chunks.Add(txtChunk);

                    }
                }
            }

            if (chunks.Count > 0)
            {
                await sender.SendAsync(() => ProcessAsync(vectorDb, embedding, [.. chunks], cancellationToken));
            }

            foreach (var log in Logs)
            {
                logger.LogInformation("log: {logText}, source: {logSource}", log.Text, log.Source);
            }
        }

        logger.LogInformation("total records: {totalRecordsToProcess} to process", totalRecords);

        this.ingestionReporter.Init(totalRecords);

        sender.Complete();
        await sender.Completion;
    }

    private async Task ProcessAsync(IVectorDb vectorDb, IEmbedding embedding, TextChunk[] chunkBatch, CancellationToken cancellationToken)
    {
        try
        {
            this.ingestionReporter.IncrementSearchModelsProcessing(chunkBatch.Length);
            this.ingestionReporter.IncrementEmbeddingHttpRequest();
            var floatsList = await embedding.GetEmbeddingsAsync(chunkBatch.Select(
                x => x.Text ?? throw new Exception("text is null")).ToArray(), cancellationToken);

            List<SearchModel> models = [];
            for (int i = 0; i < chunkBatch.Length; i++)
            {
                var t = chunkBatch[i];
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
            logger.LogError(ex, "error processing page");
        }
    }
}
