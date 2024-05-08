using AIOChatbot.Configurations;
using AIOChatbot.Ingestions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharpToken;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;

namespace AIOChatbot.Commands;

public class IngestCommand : ICommandAction
{
    private readonly IEnumerable<IIngestionDataSource> vectorDbIngestions;
    private readonly IngestionReporter ingestionReporter;
    private readonly ILogger<IngestCommand> logger;
    private readonly IConfig config;
    private readonly IEnumerable<IVectorDb> vectorDbs;
    private readonly IEnumerable<IIngestionProcessor> ingestionProcessors;
    public IngestCommand(IEnumerable<IIngestionDataSource> vectorDbIngestions,
        IEnumerable<IVectorDb> vectorDbs,
        IngestionReporter ingestionReporter,
        ILogger<IngestCommand> logger,
        IEnumerable<IIngestionProcessor> ingestionProcessors,
        IConfig config)
    {
        this.vectorDbIngestions = vectorDbIngestions;
        this.ingestionReporter = ingestionReporter;
        this.logger = logger;
        this.config = config;
        this.vectorDbs = vectorDbs;
        this.ingestionProcessors = ingestionProcessors;
    }

    public string Name => "ingest";

    public async Task ExecuteAsync(IConfiguration argsConfiguration, CancellationToken cancellationToken)
    {
        var vectorDb = vectorDbs.GetSelectedVectorDb();
        var ingestionProcessor = ingestionProcessors.GetIngestionProcessor(config);
        logger.LogInformation("initializing vectordb");
        await vectorDb.InitAsync();

        Stopwatch timeLoading = new();
        timeLoading.Start();
        ConcurrentBag<SearchModelDto> all = [];

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

        foreach (var (start, end) in indexRanges)
        {
            await senderProcessor.SendAsync(async () =>
            {
                var searchModels = all.GetSearchModels(start, end);
                await ingestionProcessor.ProcessAsync(searchModels, config.CollectionName, cancellationToken);
            });
        }

        senderProcessor.Complete();
        await senderProcessor.Completion;

        timeProcessing.Stop();
        logger.LogInformation("processing data took {timeProcessingInMilliseconds} ms", timeProcessing.ElapsedMilliseconds);
    }

    private static readonly GptEncoding gptEncoding = GptEncoding.GetEncoding(Model.GetEncodingNameForModel(Environment.GetEnvironmentVariable("TextEmbeddingName")));
}
