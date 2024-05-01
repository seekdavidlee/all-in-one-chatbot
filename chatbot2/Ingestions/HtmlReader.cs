using Azure.Storage.Blobs;
using chatbot2.Configuration;
using chatbot2.Logging;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace chatbot2.Ingestions;

public class HtmlReader
{
    private readonly string pageContentXPath;
    private readonly Dictionary<string, BlobContainerClient> containerClients = [];
    private readonly IConfig config;
    private readonly ILogger logger;

    public HtmlReader(IConfig config, ILogger logger)
    {
        pageContentXPath = Environment.GetEnvironmentVariable("PageContentXPath") ?? throw new Exception("Missing PageContentXPath");
        this.config = config;
        this.logger = logger;
    }

    public async Task<(List<Page> Pages, List<PageLogEntry> Logs)> ReadBlobsAsync(string sourceDirectory, CancellationToken cancellationToken)
    {
        using var readBlobs = DiagnosticServices.Source.StartActivity("ReadBlobsAsync");
        readBlobs?.AddTag("sourceDirectory", sourceDirectory);
        var logs = new List<PageLogEntry>();
        List<Page> pages = [];

        var containerName = sourceDirectory.Split('/')[0];

        if (!containerClients.TryGetValue(containerName, out BlobContainerClient? blobContainerClient))
        {
            blobContainerClient = new BlobContainerClient(config.AzureStorageConnectionString, containerName);
        }

        int totalBlobs = 0;
        int totalBlobsWithValidContent = 0;
        await foreach (var blob in blobContainerClient.GetBlobsAsync(
            prefix: sourceDirectory[(containerName.Length + 1)..], cancellationToken: cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return (pages, logs);
            }

            if (!blob.Name.EndsWith(".htm"))
            {
                continue;
            }

            var bc = new BlobClient(config.AzureStorageConnectionString, containerName, blob.Name);
            var content = (await bc.DownloadContentAsync(cancellationToken)).Value.Content.ToString();

            var path = bc.Uri.ToString();
            var page = GetPage(content, path[..path.IndexOf('?')], logs);
            if (page is not null)
            {
                pages.Add(page);
                totalBlobsWithValidContent++;
            }
            totalBlobs++;
        }
        readBlobs?.AddTag("totalBlobs", totalBlobs);
        readBlobs?.AddTag("totalBlobsWithValidContent", totalBlobsWithValidContent);

        logger.LogInformation("{blobSource} Total: {totalFiles} Valid Total: {totalFilesWithValidContent}", sourceDirectory, totalBlobs, totalBlobsWithValidContent);

        return (pages, logs);
    }

    public async Task<(List<Page> Pages, List<PageLogEntry> Logs)> ReadFilesAsync(string sourceDirectory, CancellationToken cancellationToken)
    {
        using var readBlobs = DiagnosticServices.Source.StartActivity("ReadFilesAsync");
        readBlobs?.AddTag("sourceDirectory", sourceDirectory);

        var stats = new FileReadStats();
        var logs = new List<PageLogEntry>();
        List<Page> pages = [];
        await InternalReadFilesAsync(sourceDirectory, pages, logs, stats, cancellationToken);

        readBlobs?.AddTag("totalFiles", stats.Total);
        readBlobs?.AddTag("totalFilesWithValidContent", stats.TotalValidContent);

        logger.LogInformation("{fileSource} Total: {totalFiles} Valid Total: {totalFilesWithValidContent}", sourceDirectory, stats.Total, stats.TotalValidContent);

        return (pages, logs);
    }

    private async Task InternalReadFilesAsync(string sourceDirectory, List<Page> pages, List<PageLogEntry> logs, FileReadStats stats, CancellationToken cancellationToken)
    {
        var dirs = Directory.GetDirectories(sourceDirectory);
        foreach (var dir in dirs)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            await InternalReadFilesAsync(dir, pages, logs, stats, cancellationToken);
        }

        foreach (var filePath in Directory.GetFiles(sourceDirectory, "*.htm"))
        {
            stats.Total++;
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            var page = await ReadFileAsync(filePath, logs);
            if (page is null)
            {
                continue;
            }
            stats.TotalValidContent++;
            pages.Add(page);
        };
    }

    public async Task<Page?> ReadFileAsync(string filePath, List<PageLogEntry> logs)
    {
        using var reader = new StreamReader(filePath);
        return GetPage(await reader.ReadToEndAsync(), filePath, logs);
    }

    private Page? GetPage(string html, string source, List<PageLogEntry> logs)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var node = doc.DocumentNode.SelectSingleNode(pageContentXPath);
        if (node is null)
        {
            logs.Add(new PageLogEntry { Source = source, Text = $"{pageContentXPath} not found" });
            return default;
        }
        Page page = new(node, new PageContext { PagePath = source }, logs);
        page.Process();
        return page;
    }

    private class FileReadStats
    {
        public int Total { get; set; } = 0;
        public int TotalValidContent { get; set; } = 0;
    }
}
