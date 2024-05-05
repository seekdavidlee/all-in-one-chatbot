namespace AIOChatbot.Configuration;

public interface IConfig
{
    string AzureOpenAIEmbeddings { get; }
    string AzureSearchKey { get; }
    string AzureSearchEndpoint { get; }
    string AzureOpenAIKey { get; }
    string CustomAuthProviderUrl { get; }
    string CustomAuthProviderContent { get; }
    string AzureStorageConnectionString { get; }
    int TextEmbeddingVectorDimension { get; }
    public string OpenTelemetryConnectionString { get; }
    public int IngestionQueuePollingInterval { get; }
    public string[] IngestionTypes { get; }
    string LogLevel { get; }
    int Concurrency { get; }
    int IngestionBatchSize { get; }
    int MessageDequeueCount { get; }
    public int IngestionReportEveryXSeconds { get; }
    string IngestionQueueName { get; }
    string EvaluationQueueName { get; }
    string IngestionProcessorType { get; }
    string InferenceProcessorType { get; }
    string IngestionQueueStorageName { get; }
    string InferenceQueueName { get; }
    string InferenceResponseQueueName { get; }
    string CollectionName { get; }
    string AzureQueueConnectionString { get; }
    string EmbeddingType { get; }
    string EvaluationStorageName { get; }
    string GroundTruthStorageName { get; }
    string ProjectStorageName { get; }
    void Validate();
}
