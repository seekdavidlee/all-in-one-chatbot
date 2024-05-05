using System.Diagnostics.Metrics;
using System.Diagnostics;

namespace AIOChatbot.Logging;

public static class DiagnosticServices
{
    const string SourceName = "Bot";
    public static readonly ActivitySource Source = new(SourceName);
    static readonly Meter Metrics = new(SourceName);
    static readonly Histogram<int> EmbeddingTokenCount = Metrics.CreateHistogram<int>("embedding_token_count", "token", "Total embedding tokens");
    static readonly Histogram<double> EmbeddingTokensPerSecond = Metrics.CreateHistogram<double>("embedding_tokens_per_sec", "sec", "Embedding tokens per second");

    public static void RecordEmbeddingTokens(int tokens, double totalTimeInMs, int textListCount, string modelName)
    {
        var modelTag = new KeyValuePair<string, object?>("model", modelName);
        var textListCountTag = new KeyValuePair<string, object?>("textListCount", textListCount);
        var totalTimeInMsTag = new KeyValuePair<string, object?>("totalTimeInMs", totalTimeInMs);
        EmbeddingTokenCount.Record(tokens, modelTag, textListCountTag, totalTimeInMsTag);
    }

    public static void RecordEmbeddingTokensPerSecond(double tokensPerSecond, double totalTimeInMs, int textListCount, string modelName)
    {
        var modelTag = new KeyValuePair<string, object?>("model", modelName);
        var textListCountTag = new KeyValuePair<string, object?>("textListCount", textListCount);
        var totalTimeInMsTag = new KeyValuePair<string, object?>("totalTimeInMs", totalTimeInMs);
        EmbeddingTokensPerSecond.Record(tokensPerSecond, modelTag, textListCountTag, totalTimeInMsTag);
    }
}

