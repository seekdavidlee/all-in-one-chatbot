using ChromaDBSharp.Client;
using System.Text;

namespace chatbot2.VectorDbs;

public class ChromaDbClient : IVectorDb, IDisposable
{
    private readonly ChromaDBClient client;
    private readonly HttpClient httpClient;
    private ICollectionClient? collectionClient;
    private readonly IEmbedding embedding;
    private readonly string collectionName;
    private readonly float minimumScore = 0.8f;

    public ChromaDbClient(IEnumerable<IEmbedding> embeddings)
    {
        embedding = embeddings.GetSelectedEmbedding();
        collectionName = Environment.GetEnvironmentVariable("CollectionName") ?? throw new Exception("Missing CollectionName");
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
            await collectionClient.AddAsync(
                ids: [textChunk.Id],
                embeddings: [embeddings],
                metadatas: [textChunk.MetaDatas],
                documents: [textChunk.Text]);
        }
    }

    public Task DeleteAsync()
    {
        if (collectionClient is null)
        {
            throw new Exception("CollectionClient is not initialized!");
        }
        return collectionClient.DeleteAsync([collectionName]);
    }

    public async Task<IEnumerable<IndexedDocument>> SearchAsync(string searchText)
    {
        if (collectionClient is null)
        {
            throw new Exception("CollectionClient is not initialized!");
        }
        var embeddings = await embedding.GetEmbeddingsAsync(searchText);
        var results = await collectionClient.QueryAsync(queryEmbeddings: new[] { embeddings }, numberOfResults: 5);
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

public class IndexedDocument
{
    public string? Id { get; set; }
    public float? Score { get; set; }
    public IDictionary<string, string>? MetaDatas { get; set; }

    public string? Text { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Id: {Id}");
        sb.AppendLine($"Score: {Score}");

        if (MetaDatas is not null)
        {
            sb.AppendLine("MetaData:");
            foreach (var m in MetaDatas)
            {
                sb.AppendLine($"{m.Key}: {m.Value}");
            }
        }

        if (Text is not null)
        {
            sb.AppendLine($"DocumentText: {Text}");
        }

        return sb.ToString();
    }
}
