namespace chatbot2.Configuration;

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
    public int IngestionReportEveryXSeconds { get; }
    string IngestionQueueName { get; }
    string IngestionProcessorType { get; }
    string IngestionQueueStorageName { get; }
    string CollectionName { get; }
    string AzureQueueConnectionString { get; }
    string EmbeddingType { get; }
    string EvaluationStorageName { get; }
    string GroundTruthStorageName { get; }
    void Validate();
}
