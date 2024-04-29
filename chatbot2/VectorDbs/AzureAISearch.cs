using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using System.Text.Json;

namespace chatbot2.VectorDbs;

public class AzureAISearch : IVectorDb
{
    private readonly string collectionName;
    private readonly SearchClient searchClient;
    private readonly SearchIndexClient searchIndexClient;
    private readonly IEmbedding embedding;
    public AzureAISearch(IEnumerable<IEmbedding> embeddings)
    {
        embedding = embeddings.GetSelectedEmbedding();
        collectionName = Environment.GetEnvironmentVariable("CollectionName") ?? throw new Exception("Missing CollectionName");
        Uri azureSearchEndpoint = new(Environment.GetEnvironmentVariable("AzureSearchEndpoint") ?? throw new Exception("Missing AzureSearchEndpoint"));
        var keyCredentials = new AzureKeyCredential(Environment.GetEnvironmentVariable("AzureSearchKey") ?? throw new Exception("Missing AzureSearchKey"));
        searchIndexClient = new(azureSearchEndpoint, keyCredentials);
        searchClient = new SearchClient(azureSearchEndpoint, collectionName, keyCredentials);
    }

    public Task DeleteAsync()
    {
        return searchIndexClient.DeleteIndexAsync(collectionName);
    }

    const int VECTOR_DIMENSION = 1536;
    const string VECTOR_PROFILE_NAME = "my-profile-config";
    const string VECTOR_ALG_NAME = "my-alg-config";

    public async Task InitAsync()
    {
        var searchIndex = new SearchIndex(collectionName);
        searchIndex.Fields.Add(new SimpleField("id", SearchFieldDataType.String) { IsKey = true });
        searchIndex.Fields.Add(new VectorSearchField("contentVector", VECTOR_DIMENSION, VECTOR_PROFILE_NAME));
        searchIndex.Fields.Add(new SimpleField("meta_json_string", SearchFieldDataType.String));
        searchIndex.Fields.Add(new SearchField("title", SearchFieldDataType.String) { IsSearchable = true });
        searchIndex.Fields.Add(new SimpleField("filepath", SearchFieldDataType.String));
        searchIndex.Fields.Add(new SimpleField("url", SearchFieldDataType.String));
        searchIndex.Fields.Add(new SearchField("content", SearchFieldDataType.String) { IsSearchable = true });

        searchIndex.VectorSearch = new VectorSearch();
        searchIndex.VectorSearch.Algorithms.Add(new HnswAlgorithmConfiguration(VECTOR_ALG_NAME));
        searchIndex.VectorSearch.Profiles.Add(new VectorSearchProfile(VECTOR_PROFILE_NAME, VECTOR_ALG_NAME));

        await searchIndexClient.CreateOrUpdateIndexAsync(searchIndex);
    }

    public Task ProcessAsync(IEnumerable<SearchModel> models)
    {
        var batch = IndexDocumentsBatch.Upload(models);
        return searchClient.IndexDocumentsAsync(batch);
    }

    public async Task<IEnumerable<IndexedDocument>> SearchAsync(string searchText, CancellationToken cancellationToken)
    {
        var embeddings = (await embedding.GetEmbeddingsAsync([searchText], cancellationToken)).Single();

        var query = new VectorizedQuery(embeddings);
        query.Fields.Add("contentVector");
        query.KNearestNeighborsCount = 5;

        var vectorSearchOptions = new VectorSearchOptions();
        vectorSearchOptions.Queries.Add(query);

        var searchOptions = new SearchOptions
        {
            VectorSearch = vectorSearchOptions
        };

        var results = await searchClient.SearchAsync<SearchModel>(searchOptions);

        List<IndexedDocument> indexedDocuments = [];
        await foreach (var result in results.Value.GetResultsAsync())
        {
            if (result.Score is null)
            {
                continue;
            }

            indexedDocuments.Add(new IndexedDocument
            {
                Score = (float)result.Score,
                Id = result.Document.Id,
                MetaDatas = result.Document.MetaData is not null ?
                    JsonSerializer.Deserialize<IDictionary<string, string>>(result.Document.MetaData) :
                    default,
                Text = result.Document.Content,
            });
        }

        return indexedDocuments.OrderByDescending(x => x.Score);
    }
}
