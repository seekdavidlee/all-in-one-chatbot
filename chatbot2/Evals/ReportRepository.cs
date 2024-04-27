using Azure.Storage.Blobs;
using System.Text;
using System.Text.Json;

namespace chatbot2.Evals;

public class ReportRepository
{
    private readonly BlobContainerClient client;

    public ReportRepository()
    {
        var connectionString = Environment.GetEnvironmentVariable("AzureStorageConnectionString") ?? throw new Exception("missing AzureStorageConnectionString");
        var svc = new BlobServiceClient(connectionString);
        var containerName = Environment.GetEnvironmentVariable("AzureStorageContainerName") ?? throw new Exception("missing AzureStorageContainerName");
        client = svc.GetBlobContainerClient(containerName);
    }

    public async Task SaveAsync<T>(string name, T item) where T : class
    {
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, item);
        stream.Position = 0;
        await UploadAsync(name, stream);
    }

    public Task UploadAsync(string name, Stream stream)
    {
        var blob = client.GetBlobClient(name);
        return blob.UploadAsync(stream);
    }
}
