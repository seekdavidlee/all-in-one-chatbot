using Azure.Storage.Blobs;
using chatbot2.Configuration;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace chatbot2.Evals;

public class ReportRepository
{
    private readonly BlobContainerClient client;

    public ReportRepository(IConfig config)
    {
        var connectionString = config.AzureStorageConnectionString;
        var svc = new BlobServiceClient(connectionString);
        var containerName = Environment.GetEnvironmentVariable("AzureStorageContainerName") ?? throw new Exception("missing AzureStorageContainerName");
        client = svc.GetBlobContainerClient(containerName);
    }

    public async Task SaveAsync<T>(string name, T item) where T : class
    {
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, item, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        stream.Position = 0;
        await UploadAsync(name, stream);
    }

    public Task UploadAsync(string name, Stream stream)
    {
        var blob = client.GetBlobClient(name);
        return blob.UploadAsync(stream, overwrite: true);
    }

    public async IAsyncEnumerable<(T Item, string BlobName)> GetAsync<T>(string path)
    {
        await foreach (var b in client.GetBlobsAsync(prefix: path))
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
        var client = this.client.GetBlobClient(name);
        var content = await client.DownloadContentAsync();
        return await JsonSerializer.DeserializeAsync<T>(content.Value.Content.ToStream());
    }
}
