using chatbot2.Configuration;
using chatbot2.Ingestions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharpToken;
using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;

namespace chatbot2.Commands;

public class IngestCommand : ICommandAction
{
    private readonly IEnumerable<IVectorDbIngestion> vectorDbIngestions;
    private readonly IngestionReporter ingestionReporter;
    private readonly ILogger<IngestCommand> logger;
    private readonly IConfig config;
    private readonly IVectorDb vectorDb;
    private readonly IEmbedding embedding;

    public IngestCommand(IEnumerable<IVectorDbIngestion> vectorDbIngestions,
        IEnumerable<IVectorDb> vectorDbs,
        IEnumerable<IEmbedding> embeddings,
        IngestionReporter ingestionReporter,
        ILogger<IngestCommand> logger,
        IConfig config)
    {
        this.vectorDbIngestions = vectorDbIngestions;
        this.ingestionReporter = ingestionReporter;
        this.logger = logger;
        this.config = config;
        vectorDb = vectorDbs.GetSelectedVectorDb();
        embedding = embeddings.GetSelectedEmbedding();
    }

    public string Name => "ingest";

    public async Task ExecuteAsync(IConfiguration argsConfiguration, CancellationToken cancellationToken)
    {
        logger.LogInformation("initializing vectordb");
        await vectorDb.InitAsync();

        ConcurrentBag<SearchModel> all = [];

        logger.LogInformation("loading data");
        var senderLoader = new ActionBlock<Func<Task>>((action) => action(), config.GetDataflowOptions(cancellationToken, vectorDbIngestions.Count()));
        foreach (var ingestion in vectorDbIngestions)
        {
            await senderLoader.SendAsync(async () =>
            {
                var items = await ingestion.LoadDataAsync(cancellationToken);
                foreach (var item in items)
                {
                    all.Add(item);
                }
            });
        }

        senderLoader.Complete();
        await senderLoader.Completion;

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        logger.LogInformation("total records: {totalRecordsToProcess} to process", all.Count);

        using var timer = new Timer((o) => this.ingestionReporter.Report(),
            null, TimeSpan.FromSeconds(config.IngestionReportEveryXSeconds), TimeSpan.FromSeconds(config.IngestionReportEveryXSeconds));
        this.ingestionReporter.Init(all.Count);

        var senderProcessor = new ActionBlock<Func<Task>>((action) => action(), config.GetDataflowOptions(cancellationToken));
        int size = 0;
        var batches = new List<SearchModel>();
        foreach (var model in all)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            int currentSize = gptEncoding.CountTokens(model.ContentToVectorized);
            if (size + currentSize > config.IngestionBatchSize)
            {
                await senderProcessor.SendAsync(() => ProcessAsync(vectorDb, embedding, batches, cancellationToken));
                size = 0;
                batches.Clear();
            }

            size += currentSize;
            batches.Add(model);
        }

        if (batches.Count > 0)
        {
            await senderProcessor.SendAsync(() => ProcessAsync(vectorDb, embedding, batches, cancellationToken));
        }

        senderProcessor.Complete();
        await senderProcessor.Completion;
    }

    private async Task ProcessAsync(IVectorDb vectorDb, IEmbedding embedding, List<SearchModel> searchModels, CancellationToken cancellationToken)
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

            var (successCount, errorCount) = await vectorDb.ProcessAsync(searchModels);
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
            logger.LogError(ex, "error processing chunkBatch");
            this.ingestionReporter.IncrementSearchModelsErrored(searchModels.Count);
        }
    }

    private static readonly GptEncoding gptEncoding = GptEncoding.GetEncoding(Model.GetEncodingNameForModel(Environment.GetEnvironmentVariable("TextEmbeddingName")));
}
