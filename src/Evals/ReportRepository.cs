using Azure.Storage.Blobs;
using AIOChatbot.Configuration;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace AIOChatbot.Evals;

public class ReportRepository
{
    private BlobContainerClient? client;
    private readonly IConfig config;
    private readonly object reportLock = new();
    public ReportRepository(IConfig config)
    {
        this.config = config;
    }

    private BlobContainerClient GetClient()
    {
        lock (reportLock)
        {
            if (client is null)
            {
                var svc = new BlobServiceClient(config.AzureStorageConnectionString);
                client = svc.GetBlobContainerClient(config.EvaluationStorageName);
            }
            return client;
        }
    }

    private static readonly JsonSerializerOptions encoder = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task SaveAsync<T>(string name, T item) where T : class
    {
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, item, encoder);
        stream.Position = 0;
        await UploadAsync(name, stream);
    }

    public Task UploadAsync(string name, Stream stream)
    {
        var blob = GetClient().GetBlobClient(name);
        return blob.UploadAsync(stream, overwrite: true);
    }

    public async IAsyncEnumerable<(T Item, string BlobName)> GetAsync<T>(string path)
    {
        await foreach (var b in GetClient().GetBlobsAsync(prefix: path))
        {
            var item = await GetItemAsync<T>(b.Name);
            if (item is not null)
            {
                yield return (item, b.Name);
            }
        }
    }

    public async Task<T?> GetItemAsync<T>(string name)
    {
        var client = this.GetClient().GetBlobClient(name);
        var content = await client.DownloadContentAsync();
        return await JsonSerializer.DeserializeAsync<T>(content.Value.Content.ToStream());
    }
}
