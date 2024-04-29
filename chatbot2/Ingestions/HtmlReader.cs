using Azure.Storage.Blobs;
using chatbot2.Configuration;
using HtmlAgilityPack;

namespace chatbot2.Ingestions;

public class HtmlReader
{
    private readonly string pageContentXPath;
    private readonly Dictionary<string, BlobContainerClient> containerClients = new();
    private readonly IConfig config;

    public HtmlReader(IConfig config)
    {
        pageContentXPath = Environment.GetEnvironmentVariable("PageContentXPath") ?? throw new Exception("Missing PageContentXPath");
        this.config = config;
    }

    public async Task<(List<Page> Pages, List<PageLogEntry> Logs)> ReadBlobsAsync(string sourceDirectory, CancellationToken cancellationToken)
    {
        var logs = new List<PageLogEntry>();
        List<Page> pages = [];

        var containerName = sourceDirectory.Split('/')[0];

        if (!containerClients.TryGetValue(containerName, out BlobContainerClient? blobContainerClient))
        {
            blobContainerClient = new BlobContainerClient(config.AzureStorageConnectionString, containerName);
        }

        await foreach (var blob in blobContainerClient.GetBlobsAsync(
            prefix: sourceDirectory[(containerName.Length + 1)..], cancellationToken: cancellationToken))
        {
            var bc = new BlobClient(config.AzureStorageConnectionString, containerName, blob.Name);
            var content = await bc.DownloadContentAsync(cancellationToken);
            using var reader = new StreamReader(content.Value.Content.ToStream());
            var page = GetPage(await reader.ReadToEndAsync(cancellationToken), bc.Uri.ToString(), logs);
            if (page is not null)
            {
                pages.Add(page);
            }
        }

        return (pages, logs);
    }

    public async Task<(List<Page> Pages, List<PageLogEntry> Logs)> ReadFilesAsync(string sourceDirectory, CancellationToken cancellationToken)
    {
        var logs = new List<PageLogEntry>();
        List<Page> pages = [];
        await InternalReadFilesAsync(sourceDirectory, pages, logs);
        return (pages, logs);
    }

    private async Task InternalReadFilesAsync(string sourceDirectory, List<Page> pages, List<PageLogEntry> logs)
    {
        var dirs = Directory.GetDirectories(sourceDirectory);
        foreach (var dir in dirs)
        {
            await InternalReadFilesAsync(dir, pages, logs);
        }

        foreach (var filePath in Directory.GetFiles(sourceDirectory, "*.htm"))
        {
            var page = await ReadFileAsync(filePath, logs);
            if (page is null)
            {
                continue;
            }
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
}
