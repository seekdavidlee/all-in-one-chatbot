using Azure.Storage.Blobs;
using AIOChatbot.Configurations;
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

    public const string PromptsResourcePrefix = "prompts_resource:";

    public async Task<string> GetFileContentAsync(string filePath, CancellationToken cancellationToken, bool useCache = true)
    {
        if (useCache && fileContents.TryGetValue(filePath, out var content))
        {
            return content;
        }

        if (filePath.StartsWith(PromptsResourcePrefix))
        {
            var promptResourceContent = await Util.GetResourceAsync(filePath[PromptsResourcePrefix.Length..]);
            if (useCache)
            {
                fileContents.TryAdd(filePath, promptResourceContent);
            }
            return promptResourceContent;
        }

        if (filePath.StartsWith("blob:"))
        {
            var length = "blob:".Length;
            var split = filePath[length..].Split('/');

            var path = filePath[(length + split[0].Length)..];
            var blob = new BlobClient(config.AzureStorageConnectionString, split[0], path);
            var response = await blob.DownloadContentAsync(cancellationToken);

            var blobContent = response.Value.Content.ToString();
            if (useCache)
            {
                fileContents.TryAdd(filePath, blobContent);
            }
            return blobContent;
        }

        var fileContent = await File.ReadAllTextAsync(filePath, cancellationToken);
        if (useCache)
        {
            fileContents.TryAdd(filePath, fileContent);
        }
        return fileContent;
    }
}

