using NetBricks;

namespace AIOChatbot.Configurations;
public class Config : IConfig
{
    private readonly NetBricks.IConfig config;
    public Config(NetBricks.IConfig config)
    {
        this.config = config;
        this.AzureOpenAIEmbeddings = this.config.GetSecret<string>("AzureOpenAIEmbeddings").GetAwaiter().GetResult();

        var azureSearchConnectionStringResult = this.config.GetSecret<string>("AzureSearchConnectionString").GetAwaiter().GetResult();
        if (azureSearchConnectionStringResult is not null)
        {
            var conn = azureSearchConnectionStringResult.Split(';');
            this.AzureSearchKey = conn[1];
            this.AzureSearchEndpoint = conn[0];
        }
        else
        {
            this.AzureSearchKey = string.Empty;
            this.AzureSearchEndpoint = string.Empty;
        }
        this.ChatbotHttpEndpoint = this.config.Get<string>("ChatbotHttpEndpoint");
        this.AzureOpenAIEndpoint = this.config.Get<string>("AzureOpenAIEndpoint");
        this.AzureOpenAILLMDeploymentModel = this.config.Get<string>("AzureOpenAILLMDeploymentModel");
        this.AzureOpenAIKey = this.config.GetSecret<string>("AzureOpenAIKey").GetAwaiter().GetResult();
        this.CustomAuthProviderUrl = this.config.GetSecret<string>("CustomAuthProviderUrl").GetAwaiter().GetResult();
        this.CustomAuthProviderContent = this.config.GetSecret<string>("CustomAuthProviderContent").GetAwaiter().GetResult();
        this.AzureStorageConnectionString = this.config.GetSecret<string>("AzureStorageConnectionString").GetAwaiter().GetResult();
        this.OpenTelemetryConnectionString = this.config.Get<string>("OpenTelemetryConnectionString");
        this.LogLevel = this.config.Get<string>("LogLevel").AsString(() => "Information");
        this.IngestionTypes = this.config.Get("IngestionTypes", (list) => list is null ? [] : list.Split(','));
        this.InferenceWorkflowSteps = this.config.Get("InferenceWorkflowSteps", (list) => list is null ? [] : list.Split(','));
        this.TextEmbeddingVectorDimension = this.config.Get<int>("TextEmbeddingVectorDimension", v => v is null ? 0 : int.Parse(v));
        this.Concurrency = this.config.Get<int>("Concurrency", v => v is null ? 3 : int.Parse(v));
        this.IngestionBatchSize = this.config.Get<int>("IngestionBatchSize", v => v is null ? 5000 : int.Parse(v));
        this.IngestionReportEveryXSeconds = this.config.Get<int>("IngestionReportEveryXSeconds", v => v is null ? 15 : int.Parse(v));
        this.IngestionQueueName = this.config.Get<string>("IngestionQueueName");
        this.EvaluationQueueName = this.config.Get<string>("EvaluationQueueName");
        this.IngestionProcessorType = this.config.Get<string>("IngestionProcessorType");
        this.IngestionQueueStorageName = this.config.Get<string>("IngestionQueueStorageName");
        this.AzureQueueConnectionString = this.config.GetSecret<string>("AzureQueueConnectionString").GetAwaiter().GetResult();
        this.CollectionName = this.config.Get<string>("CollectionName");
        this.EmbeddingType = this.config.Get<string>("EmbeddingType");
        this.IngestionQueuePollingInterval = this.config.Get<int>("IngestionQueuePollingInterval", v => v is null ? 1000 : int.Parse(v));
        this.EvaluationStorageName = this.config.Get<string>("EvaluationStorageName");
        this.GroundTruthStorageName = this.config.Get<string>("GroundTruthStorageName");
        this.ProjectStorageName = this.config.Get<string>("ProjectStorageName");
        this.InferenceQueueName = this.config.Get<string>("InferenceQueueName");
        this.InferenceResponseQueueName = this.config.Get<string>("InferenceResponseQueueName");
        this.InferenceProcessorType = this.config.Get<string>("InferenceProcessorType");
        this.MessageDequeueCount = this.config.Get<int>("MessageDequeueCount", v => v is null ? 5 : int.Parse(v));
    }
    public string ChatbotHttpEndpoint { get; }
    public string AzureOpenAIEndpoint { get; }
    public string AzureOpenAIEmbeddings { get; }
    public string AzureSearchKey { get; }
    public string AzureSearchEndpoint { get; }
    public string AzureOpenAIKey { get; }
    public string AzureOpenAILLMDeploymentModel { get; }
    public string CustomAuthProviderUrl { get; }
    public string CustomAuthProviderContent { get; }
    public string AzureStorageConnectionString { get; }
    public string OpenTelemetryConnectionString { get; }
    public int IngestionQueuePollingInterval { get; }
    public string InferenceQueueName { get; }
    public string IngestionQueueName { get; }
    public string EvaluationQueueName { get; }
    public string InferenceResponseQueueName { get; }
    public string AzureQueueConnectionString { get; }
    public string[] IngestionTypes { get; }
    public string[] InferenceWorkflowSteps { get; }
    public string LogLevel { get; }
    public int TextEmbeddingVectorDimension { get; }
    public int IngestionBatchSize { get; }
    public int Concurrency { get; }
    public int IngestionReportEveryXSeconds { get; }
    public int MessageDequeueCount { get; }
    public string IngestionQueueStorageName { get; }
    public string IngestionProcessorType { get; }
    public string CollectionName { get; }
    public string EmbeddingType { get; }
    public string EvaluationStorageName { get; }
    public string GroundTruthStorageName { get; }
    public string ProjectStorageName { get; }
    public string InferenceProcessorType { get; }

    public void Validate()
    {
        this.config.Optional("AzureOpenAIEndpoint", this.AzureOpenAIEndpoint, hideValue: false);
        this.config.Optional("AzureOpenAILLMDeploymentModel", this.AzureOpenAILLMDeploymentModel, hideValue: false);
        this.config.Optional("AzureOpenAIEmbeddings", this.AzureOpenAIEmbeddings, hideValue: true);
        this.config.Optional("AzureSearchKey", this.AzureSearchKey, hideValue: true);
        this.config.Optional("AzureSearchEndpoint", this.AzureSearchEndpoint, hideValue: false);
        this.config.Optional("AzureOpenAIKey", this.AzureOpenAIKey, hideValue: true);
        this.config.Optional("CustomAuthProviderUrl", this.CustomAuthProviderUrl, hideValue: true);
        this.config.Optional("AzureStorageConnectionString", this.AzureStorageConnectionString, hideValue: true);
        this.config.Require("OpenTelemetryConnectionString", this.OpenTelemetryConnectionString, hideValue: false);
        this.config.Optional("IngestionTypes", this.IngestionTypes, hideValue: false);
        this.config.Optional("InferenceWorkflowSteps", this.InferenceWorkflowSteps, hideValue: false);
        this.config.Require("LogLevel", this.LogLevel, hideValue: false);
        this.config.Require("TextEmbeddingVectorDimension", this.TextEmbeddingVectorDimension, hideValue: false);
        this.config.Optional("Concurrency", this.Concurrency, hideValue: false);
        this.config.Optional("IngestionBatchSize", this.IngestionBatchSize, hideValue: false);
        this.config.Optional("IngestionReportEveryXSeconds", this.IngestionReportEveryXSeconds, hideValue: false);
        this.config.Optional("IngestionQueueName", this.IngestionQueueName, hideValue: false);
        this.config.Optional("EvaluationQueueName", this.EvaluationQueueName, hideValue: false);
        this.config.Optional("IngestionProcessorType", this.IngestionProcessorType, hideValue: false);
        this.config.Optional("AzureQueueConnectionString", this.AzureQueueConnectionString, hideValue: true);
        this.config.Optional("IngestionQueueStorageName", this.IngestionQueueStorageName, hideValue: false);
        this.config.Optional("CollectionName", this.CollectionName, hideValue: false);
        this.config.Optional("EmbeddingType", this.EmbeddingType, hideValue: false);
        this.config.Optional("IngestionQueuePollingInterval", this.IngestionQueuePollingInterval, hideValue: false);
        this.config.Optional("EvaluationStorageName", this.EvaluationStorageName, hideValue: false);
        this.config.Optional("GroundTruthStorageName", this.GroundTruthStorageName, hideValue: true);
        this.config.Optional("ProjectStorageName", this.ProjectStorageName, hideValue: true);
        this.config.Optional("InferenceQueueName", this.InferenceQueueName, hideValue: false);
        this.config.Optional("InferenceResponseQueueName", this.InferenceResponseQueueName, hideValue: false);
        this.config.Optional("InferenceProcessorType", this.InferenceProcessorType, hideValue: false);
        this.config.Optional("MessageDequeueCount", this.MessageDequeueCount, hideValue: false);
    }
}
