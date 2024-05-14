using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using AIOChatbot.Configurations;
using AIOChatbot.Ingestions;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace AIOChatbot.Commands;

public class ProcessQueueIngestionCommand : QueueCommandBase<SearchModelQueueMessage>
{
    private readonly IngestionReporter ingestionReporter;
    private readonly IConfig config;
    private readonly IEnumerable<IIngestionProcessor> ingestionProcessors;

    public ProcessQueueIngestionCommand(
        ILogger<ProcessQueueIngestionCommand> logger,
        IEnumerable<IIngestionProcessor> ingestionProcessors,
        IngestionReporter ingestionReporter,
        IConfig config) : base("ingest-queue-processing", config.IngestionQueueName, logger, config)
    {
        this.ingestionProcessors = ingestionProcessors;
        this.ingestionReporter = ingestionReporter;
        this.config = config;
    }

    protected override Task InitAsync()
    {
        using var timer = new Timer((o) => this.ingestionReporter.Report(), null,
            TimeSpan.FromSeconds(config.IngestionReportEveryXSeconds), TimeSpan.FromSeconds(config.IngestionReportEveryXSeconds));

        this.ingestionReporter.Init();
        return Task.CompletedTask;
    }

    protected override async Task ProcessMessageAsync(SearchModelQueueMessage message, CancellationToken cancellationToken)
    {
        var ingestionProcessor = ingestionProcessors.GetIngestionProcessor(config);
        var blob = new BlockBlobClient(config.AzureStorageConnectionString, config.IngestionQueueStorageName, $"{message.JobId}\\{message.Id}");
        var cnt = await blob.DownloadContentAsync(cancellationToken);
        var models = JsonSerializer.Deserialize<List<SearchModelDto>>(Encoding.UTF8.GetString(cnt.Value.Content.ToArray()));

        if (models is not null)
        {
            await ingestionProcessor.ProcessAsync(models, message.CollectionName ?? config.CollectionName, cancellationToken);
        }
        await blob.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
    }
}
