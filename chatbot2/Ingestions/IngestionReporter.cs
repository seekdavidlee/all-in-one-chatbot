using Microsoft.Extensions.Logging;
using System;

namespace chatbot2.Ingestions;

public class IngestionReporter
{
    public IngestionReporter(ILogger<IngestionReporter> logger)
    {
        this.logger = logger;
    }
    private readonly object reportLock = new();
    private readonly ILogger<IngestionReporter> logger;
    private int totalSearchModelsProcessed;
    private int searchModelsProcessed;
    private int searchModelsProcessing;
    private int searchModelsErrors;
    private int embeddingTokensProcessed;
    private int embeddingHttpRequests;
    private int interval;
    private int totalRecords;
    private DateTime lastReportTime = DateTime.UtcNow;
    private DateTime reporterStartTime = DateTime.UtcNow;

    public int IncrementSearchModelsProcessing(int count)
    {
        return Interlocked.Add(ref searchModelsProcessing, count);
    }

    public int IncrementSearchModelsProcessed(int count)
    {
        Interlocked.Add(ref totalSearchModelsProcessed, count);
        return Interlocked.Add(ref searchModelsProcessed, count);
    }

    public int IncrementEmbeddingHttpRequest()
    {
        return Interlocked.Increment(ref embeddingHttpRequests);
    }

    public int IncrementEmbeddingTokensProcessed(int count)
    {
        Interlocked.Add(ref embeddingTokensProcessed, count);
        return Interlocked.Add(ref embeddingTokensProcessed, count);
    }

    public int IncrementSearchModelsErrors()
    {
        return Interlocked.Increment(ref searchModelsErrors);
    }

    public void Init(int totalRecords)
    {
        reporterStartTime = DateTime.UtcNow;
        this.totalRecords = totalRecords;
    }

    public void Report()
    {
        lock (reportLock)
        {
            Interlocked.Increment(ref interval);

            var totalSpan = DateTime.UtcNow - reporterStartTime;
            var total = Interlocked.Add(ref totalSearchModelsProcessed, 0);
            double perSec = total / totalSpan.TotalSeconds;
            logger.LogInformation("SearchModel Total: {totalSearchModelsProcessed}, AvgRate: {totalSearchModelsProcessedAvg:0.00}/sec", total, perSec);

            var allTotalRecords = Interlocked.Add(ref totalRecords, 0);
            if (allTotalRecords > 0)
            {
                double progress = (total / allTotalRecords) * 100;
                logger.LogInformation("Progress: {progress}, Interval: {progressInterval}", progress, Interlocked.Add(ref interval, 0));
            }

            double totalSeconds = (DateTime.UtcNow - lastReportTime).TotalSeconds;
            lastReportTime = DateTime.UtcNow;

            var embeddingRps = Interlocked.Add(ref embeddingHttpRequests, 0) / totalSeconds;
            logger.LogInformation("embedding http requests/sec: {embeddingRps}", embeddingRps);

            int intervalTotal = Interlocked.Add(ref searchModelsProcessed, 0);
            perSec = intervalTotal / totalSeconds;
            logger.LogInformation("SearchModel Interval Total: {totalSearchModelsProcessed}, Interval AvgRate: {intervalSearchModelsProcessedAvg:0.00}/sec", intervalTotal, perSec);

            intervalTotal = Interlocked.Add(ref embeddingTokensProcessed, 0);
            perSec = intervalTotal / totalSeconds;
            logger.LogInformation("Embedding tokens Interval Total: {embeddingTokensProcessed}, Interval AvgRate: {intervalEmbeddingTokensProcessedAvg:0.00}/sec", total, perSec);

            logger.LogInformation("SearchModel: processing: {searchModelsProcessing} processed: {searchModelsProcessed}, errors: {searchModelsErrors}",
                Interlocked.Add(ref searchModelsProcessing, 0),
                Interlocked.Add(ref searchModelsProcessed, 0),
                Interlocked.Add(ref searchModelsErrors, 0));

            // clear
            Interlocked.Exchange(ref searchModelsProcessing, 0);
            Interlocked.Exchange(ref searchModelsProcessed, 0);
            Interlocked.Exchange(ref searchModelsErrors, 0);
            Interlocked.Exchange(ref embeddingTokensProcessed, 0);
            Interlocked.Exchange(ref embeddingHttpRequests, 0);
        }
    }
}
