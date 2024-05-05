using Azure.Storage.Blobs;
using AIOChatbot.Configuration;
using System.Collections.Concurrent;

namespace AIOChatbot.Evals;

public class FileCache
{
    private readonly ConcurrentDictionary<string, string> fileContents = new();
    private readonly IConfig config;

    public FileCache(IConfig config)
    {
        this.config = config;
    }

    public async Task<string> GetFileContentAsync(string filePath, CancellationToken cancellationToken)
    {
        if (fileContents.TryGetValue(filePath, out var content))
        {
            return content;
        }

        if (filePath.StartsWith("blob:"))
        {
            var length = "blob:".Length;
            var split = filePath.Substring(length).Split('/');

            var path = filePath.Substring(length + split[0].Length);
            var blob = new BlobClient(config.AzureStorageConnectionString, split[0], path);
            var response = await blob.DownloadContentAsync(cancellationToken);
            return response.Value.Content.ToString();
        }

        var fileContent = await File.ReadAllTextAsync(filePath, cancellationToken);
        fileContents.TryAdd(filePath, fileContent);
        return fileContent;
    }
}

