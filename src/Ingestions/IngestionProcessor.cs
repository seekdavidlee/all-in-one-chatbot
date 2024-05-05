using AIOChatbot.Commands;
using AIOChatbot.Configuration;
using Microsoft.Extensions.Logging;

namespace AIOChatbot.Ingestions;

public class IngestionProcessor : IIngestionProcessor
{
    private readonly IngestionReporter ingestionReporter;
    private readonly ILogger<IngestCommand> logger;
    private readonly IConfig config;
    private readonly IEnumerable<IVectorDb> vectorDbs;
    private readonly IEnumerable<IEmbedding> embeddings;

    public IngestionProcessor(IEnumerable<IVectorDb> vectorDbs,
        IEnumerable<IEmbedding> embeddings,
        IngestionReporter ingestionReporter,
        ILogger<IngestCommand> logger,
        IConfig config)
    {
        this.ingestionReporter = ingestionReporter;
        this.logger = logger;
        this.config = config;
        this.embeddings = embeddings;
        this.vectorDbs = vectorDbs;
    }

    public async Task ProcessAsync(List<SearchModelDto> searchModels, string collectionName, CancellationToken cancellationToken)
    {
        var vectorDb = vectorDbs.GetSelectedVectorDb();
        var embedding = embeddings.GetSelectedEmbedding(config);
        try
        {
            this.ingestionReporter.IncrementSearchModelsProcessing(searchModels.Count);
            this.ingestionReporter.IncrementEmbeddingHttpRequest();
            var floatsList = await embedding.GetEmbeddingsAsync(searchModels.Select(
                x => x.ContentToVectorized ?? throw new Exception("ContentToVectorized is null")).ToArray(), cancellationToken);

            for (var i = 0; i < searchModels.Count; i++)
            {
                searchModels[i].ContentVector = floatsList[i];
            }

            var (successCount, errorCount) = await vectorDb.ProcessAsync(searchModels, collectionName: collectionName, cancellationToken: cancellationToken);
            if (successCount > 0)
            {
                this.ingestionReporter.IncrementSearchModelsProcessed(successCount);
            }
            if (errorCount > 0)
            {
                this.ingestionReporter.IncrementSearchModelsErrored(errorCount);
            }

            if (successCount + errorCount != searchModels.Count)
            {
                logger.LogWarning("searchModels {searchModelsCount} does not match with indexed counts {searchModelsIndexedCounts}", searchModels.Count, successCount + errorCount);
            }

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "error processing searchModels");
            this.ingestionReporter.IncrementSearchModelsErrored(searchModels.Count);
        }
    }
}
