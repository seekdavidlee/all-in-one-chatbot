using chatbot2.Configuration;
using chatbot2.Ingestions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharpToken;
using System.Collections.Concurrent;
using System.Diagnostics;
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

        Stopwatch timeLoading = new();
        timeLoading.Start();
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
        timeLoading.Stop();
        logger.LogInformation("loading data took {timeLoadingInMilliseconds} ms", timeLoading.ElapsedMilliseconds);

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        logger.LogInformation("total records: {totalRecordsToProcess} to process", all.Count);

        Stopwatch timeProcessing = new();
        timeProcessing.Start();
        using var timer = new Timer((o) => this.ingestionReporter.Report(),
            null, TimeSpan.FromSeconds(config.IngestionReportEveryXSeconds), TimeSpan.FromSeconds(config.IngestionReportEveryXSeconds));
        this.ingestionReporter.Init(all.Count);

        var senderProcessor = new ActionBlock<Func<Task>>((action) => action(), config.GetDataflowOptions(cancellationToken));
        int size = 0;
        int startIndex = 0;
        List<(int start, int end)> indexRanges = [];
        for (int i = 0; i < all.Count; i++)
        {
            var model = all.ElementAt(i);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            int currentSize = gptEncoding.CountTokens(model.ContentToVectorized);
            if (size + currentSize > config.IngestionBatchSize)
            {
                indexRanges.Add((startIndex, i - 1));
                size = 0;
                startIndex = i;
            }

            size += currentSize;
        }

        if (startIndex < all.Count - 1)
        {
            indexRanges.Add((startIndex, all.Count - 1));
        }

        foreach (var range in indexRanges)
        {
            await senderProcessor.SendAsync(() => ProcessAsync(vectorDb, embedding, all, range.start, range.end, cancellationToken));
        }

        senderProcessor.Complete();
        await senderProcessor.Completion;

        timeProcessing.Stop();
        logger.LogInformation("processing data took {timeProcessingInMilliseconds} ms", timeProcessing.ElapsedMilliseconds);
    }

    private async Task ProcessAsync(IVectorDb vectorDb, IEmbedding embedding, ConcurrentBag<SearchModel> bagSearchModels, int startIndex, int endIndex, CancellationToken cancellationToken)
    {
        try
        {
            var searchModels = bagSearchModels.Skip(startIndex).Take(endIndex - startIndex).ToList();
            this.ingestionReporter.IncrementSearchModelsProcessing(searchModels.Count);
            this.ingestionReporter.IncrementEmbeddingHttpRequest();
            var floatsList = await embedding.GetEmbeddingsAsync(searchModels.Select(
                x => x.ContentToVectorized ?? throw new Exception("ContentToVectorized is null")).ToArray(), cancellationToken);

            for (var i = 0; i < searchModels.Count; i++)
            {
                searchModels[i].ContentVector = floatsList[i];
            }

            var (successCount, errorCount) = await vectorDb.ProcessAsync(searchModels, cancellationToken);
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
            this.ingestionReporter.IncrementSearchModelsErrored(endIndex - startIndex);
        }
    }

    private static readonly GptEncoding gptEncoding = GptEncoding.GetEncoding(Model.GetEncodingNameForModel(Environment.GetEnvironmentVariable("TextEmbeddingName")));
}
