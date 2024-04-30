using System.Diagnostics.Metrics;
using System.Diagnostics;

namespace chatbot2.Logging;

public static class DiagnosticServices
{
    const string SourceName = "Bot";
    public static readonly ActivitySource Source = new(SourceName);
    static readonly Meter Metrics = new(SourceName);
    static readonly Histogram<int> EmbeddingTokenCount = Metrics.CreateHistogram<int>("embedding_token_count", "token", "Total embedding tokens");

    public static void RecordEmbeddingTokens(int tokens, int textListCount, string modelName)
    {
        var modelTag = new KeyValuePair<string, object?>("model", modelName);
        var textListCountTag = new KeyValuePair<string, object?>("textListCount", textListCount);
        var timestampTag = new KeyValuePair<string, object?>("timestampTag", DateTime.UtcNow);
        EmbeddingTokenCount.Record(tokens, modelTag, textListCountTag, timestampTag);
    }
}

