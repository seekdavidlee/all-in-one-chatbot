using ChromaDBSharp.Client;
using ChromaDBSharp.Embeddings;
using Microsoft.SemanticKernel.Text;

namespace chatbot2;

public class VectorDbClient : IDisposable
{
    private readonly ChromaDBClient client;
    private readonly HttpClient httpClient;
    private ICollectionClient? collectionClient;
    private readonly Embedding embedding = new();
    private readonly string collectionName;

    public VectorDbClient()
    {
        collectionName = Environment.GetEnvironmentVariable("CollectionName") ?? throw new Exception("Missing CollectionName");
        httpClient = new()
        {
            BaseAddress = new Uri(Environment.GetEnvironmentVariable("ChromaDbEndpoint") ?? throw new Exception("Missing ChromaDbEndpoint"))
        };
        client = new(httpClient);

    }

    public void Dispose()
    {
        httpClient.Dispose();
    }

    public async Task InitAsync()
    {
        var cols = await client.ListCollectionsAsync();
        collectionClient = cols.Any(x => x.Name == collectionName) ?
            await client.GetCollectionAsync(collectionName) :
            await client.CreateCollectionAsync(collectionName);
    }

    public async Task ProcessAsync(PageSection pageSection)
    {
        if (collectionClient is null)
        {
            throw new Exception("CollectionClient is not initialized!");
        }

        foreach (var textChunk in pageSection.TextChunks)
        {
            if (textChunk.Text is null)
            {
                continue;
            }

            var embeddings = await embedding.GetEmbeddingsAsync(textChunk.Text);
            await collectionClient.AddAsync([textChunk.Id], new[] { embeddings }, new[] { textChunk.MetaDatas });
        }
    }

    public async Task SearchAsync(string searchText)
    {
        if (collectionClient is null)
        {
            throw new Exception("CollectionClient is not initialized!");
        }
        var embeddings = await embedding.GetEmbeddingsAsync(searchText);
        var results = await collectionClient.QueryAsync(queryEmbeddings: new[] { embeddings }, numberOfResults: 5);
        if (results is null || results.Ids is null)
        {
            return;
        }
        foreach (var idList in results.Ids)
        {
            
        }
    }
}
