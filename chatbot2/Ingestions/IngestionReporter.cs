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
    private DateTime lastReportTime = DateTime.UtcNow;
    private readonly DateTime reporterStartTime = DateTime.UtcNow;

    public int IncrementSearchModelsProcessing(int count)
    {
        return Interlocked.Add(ref searchModelsProcessing, count);
    }

    public int IncrementSearchModelsProcessed(int count)
    {
        Interlocked.Add(ref totalSearchModelsProcessed, count);
        return Interlocked.Add(ref searchModelsProcessed, count);
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

    public void Report()
    {
        lock (reportLock)
        {
            var totalSpan = DateTime.UtcNow - reporterStartTime;
            var total = Interlocked.Add(ref totalSearchModelsProcessed, 0);
            double perSec = total / totalSpan.TotalSeconds;
            logger.LogInformation("SearchModel Total: {totalSearchModelsProcessed}, AvgRate: {totalSearchModelsProcessedAvg:0.00}/sec", total, perSec);

            double totalSeconds = (DateTime.UtcNow - lastReportTime).TotalSeconds;
            lastReportTime = DateTime.UtcNow;

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
        }
    }
}
