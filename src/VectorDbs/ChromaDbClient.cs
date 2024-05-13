using Azure.AI.OpenAI;
using AIOChatbot.Configurations;
using ChromaDBSharp.Client;

namespace AIOChatbot.VectorDbs;

public class ChromaDbClient : IVectorDb, IDisposable
{
    private ChromaDBClient? client;
    private HttpClient? httpClient;
    private ICollectionClient? collectionClient;
    private readonly IEnumerable<IEmbedding> embeddingList;
    private readonly IConfig config;
    private float? minimumScore = 0.8f;

    public ChromaDbClient(IEnumerable<IEmbedding> embeddings, IConfig config)
    {
        embeddingList = embeddings;
        this.config = config;
    }

    public void Dispose()
    {
        httpClient?.Dispose();
        GC.SuppressFinalize(this);
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
        collectionClient = cols.Any(x => x.Name == config.CollectionName) ?
            await client.GetCollectionAsync(config.CollectionName) :
            await client.CreateCollectionAsync(config.CollectionName);
    }

    public async Task<(int SuccessCount, int ErrorCount)> ProcessAsync(IEnumerable<SearchModelDto> searchModels, CancellationToken cancellationToken, string? collectionName = null)
    {
        if (collectionClient is null)
        {
            throw new Exception("CollectionClient is not initialized!");
        }

        if (collectionName is not null)
        {
            throw new NotImplementedException("this is not implemented yet!");
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
        return collectionClient.DeleteAsync([config.CollectionName]);
    }

    public async Task<IEnumerable<IndexedDocument>> SearchAsync(string[] searchTexts, SearchParameters searchParameters, CancellationToken cancellationToken)
    {
        if (collectionClient is null)
        {
            throw new Exception("CollectionClient is not initialized!");
        }
        var embedding = embeddingList.GetSelectedEmbedding(config);
        var embeddings = await embedding.GetEmbeddingsAsync(searchTexts, cancellationToken);
        var results = await collectionClient.QueryAsync(queryEmbeddings: embeddings, numberOfResults: searchParameters.NumberOfResults);
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
