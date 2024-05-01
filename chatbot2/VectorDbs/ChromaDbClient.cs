using ChromaDBSharp.Client;

namespace chatbot2.VectorDbs;

public class ChromaDbClient : IVectorDb, IDisposable
{
    private ChromaDBClient? client;
    private HttpClient? httpClient;
    private ICollectionClient? collectionClient;
    private readonly IEmbedding embedding;
    private readonly string collectionName;
    private float? minimumScore = 0.8f;

    public ChromaDbClient(IEnumerable<IEmbedding> embeddings)
    {
        embedding = embeddings.GetSelectedEmbedding();
        collectionName = Environment.GetEnvironmentVariable("CollectionName") ?? throw new Exception("Missing CollectionName");
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }

    public async Task InitAsync()
    {
        httpClient = new()
        {
            BaseAddress = new Uri(Environment.GetEnvironmentVariable("ChromaDbEndpoint") ?? throw new Exception("Missing ChromaDbEndpoint"))
        };
        client = new(httpClient);

        var strMinimumScore = Environment.GetEnvironmentVariable("MinimumScore");
        if (strMinimumScore is not null)
        {
            if (float.TryParse(strMinimumScore, out float envMinimumScore))
            {
                minimumScore = envMinimumScore;
            }
        }
        var cols = await client.ListCollectionsAsync();
        collectionClient = cols.Any(x => x.Name == collectionName) ?
            await client.GetCollectionAsync(collectionName) :
            await client.CreateCollectionAsync(collectionName);
    }

    public async Task<(int SuccessCount, int ErrorCount)> ProcessAsync(IEnumerable<SearchModel> searchModels, CancellationToken cancellationToken)
    {
        if (collectionClient is null)
        {
            throw new Exception("CollectionClient is not initialized!");
        }

        int error = 0;
        int success = 0;
        foreach (var model in searchModels)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            if (model.Id is null ||
                model.ContentVector is null ||
                model.Content is null)
            {
                error++;
                continue;
            }

            await collectionClient.AddAsync(
                ids: [model.Id],
                embeddings: [model.ContentVector],
                documents: [model.Content]);
            success++;
        }

        return (success, error);
    }

    public Task DeleteAsync()
    {
        if (collectionClient is null)
        {
            throw new Exception("CollectionClient is not initialized!");
        }
        return collectionClient.DeleteAsync([collectionName]);
    }

    public async Task<IEnumerable<IndexedDocument>> SearchAsync(string searchText, CancellationToken cancellationToken)
    {
        if (collectionClient is null)
        {
            throw new Exception("CollectionClient is not initialized!");
        }
        var embeddings = await embedding.GetEmbeddingsAsync([searchText], cancellationToken);
        var results = await collectionClient.QueryAsync(queryEmbeddings: embeddings, numberOfResults: 5);
        if (results is null || results.Ids is null || results.Distances is null)
        {
            return [];
        }

        var docs = new List<IndexedDocument>();
        foreach (var idList in results.Ids)
        {
            foreach (var id in idList)
            {
                docs.Add(new IndexedDocument { Id = id });
            }
        }

        int counter = 0;
        foreach (var scoreList in results.Distances)
        {
            foreach (var score in scoreList)
            {
                docs[counter].Score = score;
                counter++;
            }
        }

        counter = 0;
        foreach (var chunckTextList in results.Documents)
        {
            foreach (var chunckText in chunckTextList)
            {
                docs[counter].Text = chunckText;
                counter++;
            }
        }

        return docs.Where(x => x.Score >= minimumScore).OrderByDescending(x => x.Score);
    }
}
