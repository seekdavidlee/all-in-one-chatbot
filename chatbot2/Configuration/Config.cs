using NetBricks;

namespace chatbot2.Configuration;
public class Config : IConfig
{
    private readonly NetBricks.IConfig config;
    public Config(NetBricks.IConfig config)
    {
        this.config = config;
        this.AzureOpenAIEmbeddings = this.config.GetSecret<string>("AzureOpenAIEmbeddings").GetAwaiter().GetResult();
        this.AzureSearchKey = this.config.GetSecret<string>("AzureSearchKey").GetAwaiter().GetResult();
        this.AzureOpenAIKey = this.config.GetSecret<string>("AzureOpenAIKey").GetAwaiter().GetResult();
        this.CustomAuthProviderUrl = this.config.GetSecret<string>("CustomAuthProviderUrl").GetAwaiter().GetResult();
        this.CustomAuthProviderContent = this.config.GetSecret<string>("CustomAuthProviderContent").GetAwaiter().GetResult();
        this.AzureStorageConnectionString = this.config.GetSecret<string>("AzureStorageConnectionString").GetAwaiter().GetResult();
        this.OpenTelemetryConnectionString = this.config.Get<string>("OpenTelemetryConnectionString");
        this.LogLevel = this.config.Get<string>("LogLevel").AsString(() => "Information");
        this.IngestionTypes = this.config.Get("IngestionTypes", (list) => list is null ? [] : list.Split(','));
        this.TextEmbeddingVectorDimension = this.config.Get<int>("TextEmbeddingVectorDimension", int.Parse);
    }
    public string AzureOpenAIEmbeddings { get; }
    public string AzureSearchKey { get; }
    public string AzureOpenAIKey { get; }
    public string CustomAuthProviderUrl { get; }
    public string CustomAuthProviderContent { get; }
    public string AzureStorageConnectionString { get; }
    public string OpenTelemetryConnectionString { get; }
    public string[] IngestionTypes { get; }
    public string LogLevel { get; }

    public int TextEmbeddingVectorDimension { get; }

    public void Validate()
    {
        this.config.Optional("AzureOpenAIEmbeddings", this.AzureOpenAIEmbeddings, hideValue: true);
        this.config.Optional("AzureSearchKey", this.AzureSearchKey, hideValue: true);
        this.config.Optional("AzureOpenAIKey", this.AzureOpenAIKey, hideValue: true);
        this.config.Optional("CustomAuthProviderUrl", this.CustomAuthProviderUrl, hideValue: true);
        this.config.Optional("AzureStorageConnectionString", this.AzureStorageConnectionString, hideValue: true);
        this.config.Require("OpenTelemetryConnectionString", this.OpenTelemetryConnectionString, hideValue: false);
        this.config.Require("IngestionTypes", this.IngestionTypes, hideValue: false);
        this.config.Require("LogLevel", this.LogLevel, hideValue: false);
        this.config.Require("TextEmbeddingVectorDimension", this.LogLevel, hideValue: false);
    }
}
