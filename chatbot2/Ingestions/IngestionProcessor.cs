using chatbot2.Commands;
using chatbot2.Configuration;
using Microsoft.Extensions.Logging;

namespace chatbot2.Ingestions;

public class IngestionProcessor : IIngestionProcessor
{
    private readonly IngestionReporter ingestionReporter;
    private readonly ILogger<IngestCommand> logger;
    private readonly IVectorDb vectorDb;
    private readonly IEmbedding embedding;

    public IngestionProcessor(IEnumerable<IVectorDb> vectorDbs,
        IEnumerable<IEmbedding> embeddings,
        IngestionReporter ingestionReporter,
        ILogger<IngestCommand> logger,
        IConfig config)
    {
        this.ingestionReporter = ingestionReporter;
        this.logger = logger;
        vectorDb = vectorDbs.GetSelectedVectorDb();
        embedding = embeddings.GetSelectedEmbedding(config);
    }

    public async Task ProcessAsync(List<SearchModelDto> searchModels, string collectionName, CancellationToken cancellationToken)
    {
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
