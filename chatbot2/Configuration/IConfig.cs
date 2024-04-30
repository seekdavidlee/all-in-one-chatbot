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
    void Validate();
}
