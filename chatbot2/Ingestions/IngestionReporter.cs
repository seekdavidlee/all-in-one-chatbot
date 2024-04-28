using Microsoft.Extensions.Logging;

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

    public int IncrementSearchModelsProcessing(int count)
    {
        return Interlocked.Add(ref searchModelsProcessing, count);
    }

    public int IncrementSearchModelsProcessed(int count)
    {
        Interlocked.Add(ref totalSearchModelsProcessed, count);
        return Interlocked.Add(ref searchModelsProcessed, count);
    }

    public int IncrementSearchModelsErrors()
    {
        return Interlocked.Increment(ref searchModelsErrors);
    }

    public void Report(DateTime utcStarted)
    {
        lock (reportLock)
        {
            TimeSpan span = DateTime.UtcNow - utcStarted;
            if (span.TotalSeconds > 0)
            {
                var total = Interlocked.Add(ref totalSearchModelsProcessed, 0);
                double perSec = total / span.TotalSeconds;
                logger.LogInformation("SearchModel Total: {total}, Rate: {avg:0.00}/sec", total, perSec);
            }
            else
            {
                logger.LogWarning("unable to report, totalSeconds is 0");
            }

            logger.LogInformation("SearchModel: processing: {searchModelsProcessing} processed: {searchModelsProcessed}, errors: {searchModelsErrors}",
                Interlocked.Add(ref searchModelsProcessing, 0),
                Interlocked.Add(ref searchModelsProcessed, 0),
                Interlocked.Add(ref searchModelsErrors, 0));

            // clear
            Interlocked.Exchange(ref searchModelsProcessing, 0);
            Interlocked.Exchange(ref searchModelsProcessed, 0);
            Interlocked.Exchange(ref searchModelsErrors, 0);
        }
    }
}
