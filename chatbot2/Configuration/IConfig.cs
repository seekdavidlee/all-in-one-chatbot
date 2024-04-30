namespace chatbot2.Configuration;

public interface IConfig
{
    string AzureOpenAIEmbeddings { get; }
    string AzureSearchKey { get; }
    string AzureOpenAIKey { get; }
    string CustomAuthProviderUrl { get; }
    string CustomAuthProviderContent { get; }
    string AzureStorageConnectionString { get; }
    int TextEmbeddingVectorDimension { get; }
    public string OpenTelemetryConnectionString { get; }
    public string[] IngestionTypes { get; }
    string LogLevel { get; }
    int Concurrency { get; }
    int IngestionBatchSize { get; }
    void Validate();
}
