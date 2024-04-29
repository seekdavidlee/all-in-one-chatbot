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
    }
    public string AzureOpenAIEmbeddings { get; }
    public string AzureSearchKey { get; }
    public string AzureOpenAIKey { get; }
    public string CustomAuthProviderUrl { get; }
    public string CustomAuthProviderContent { get; }
    public string AzureStorageConnectionString { get; }

    public void Validate()
    {
        this.config.Optional("AzureOpenAIEmbeddings", this.AzureOpenAIEmbeddings, hideValue: true);
        this.config.Optional("AzureSearchKey", this.AzureSearchKey, hideValue: true);
        this.config.Optional("AzureOpenAIKey", this.AzureOpenAIKey, hideValue: true);
        this.config.Optional("CustomAuthProviderUrl", this.CustomAuthProviderUrl, hideValue: true);
        this.config.Optional("AzureStorageConnectionString", this.AzureStorageConnectionString, hideValue: true);
    }
}
