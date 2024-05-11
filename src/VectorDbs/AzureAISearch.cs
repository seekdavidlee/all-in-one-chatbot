using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using AIOChatbot.Configurations;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace AIOChatbot.VectorDbs;

public class AzureAISearch : IVectorDb
{
    private SearchIndexClient? searchIndexClient;
    private readonly IEnumerable<IEmbedding> embeddingList;
    private readonly IConfig config;
    private readonly ILogger<AzureAISearch> logger;

    private readonly ConcurrentDictionary<string, SearchClient> searchClients = [];

    public AzureAISearch(IEnumerable<IEmbedding> embeddings, IConfig config, ILogger<AzureAISearch> logger)
    {
        embeddingList = embeddings;
        this.config = config;
        this.logger = logger;
    }

    private SearchIndexClient GetSearchIndexClient()
    {
        if (searchIndexClient is null)
        {
            AzureKeyCredential keyCredentials = new(config.AzureSearchKey);
            searchIndexClient = new(new Uri(this.config.AzureSearchEndpoint), keyCredentials);
        }

        return searchIndexClient;
    }

    public Task DeleteAsync()
    {
        return GetSearchIndexClient().DeleteIndexAsync(config.CollectionName);
    }

    const string VECTOR_PROFILE_NAME = "my-profile-config";
    const string VECTOR_ALG_NAME = "my-alg-config";

    public async Task InitAsync()
    {
        var searchIndex = new SearchIndex(config.CollectionName);
        searchIndex.Fields.Add(new SimpleField("id", SearchFieldDataType.String) { IsKey = true });
        searchIndex.Fields.Add(new VectorSearchField("contentVector", config.TextEmbeddingVectorDimension, VECTOR_PROFILE_NAME));
        searchIndex.Fields.Add(new SimpleField("meta_json_string", SearchFieldDataType.String));
        searchIndex.Fields.Add(new SearchField("title", SearchFieldDataType.String) { IsSearchable = true });
        searchIndex.Fields.Add(new SimpleField("filepath", SearchFieldDataType.String));
        searchIndex.Fields.Add(new SimpleField("url", SearchFieldDataType.String));
        searchIndex.Fields.Add(new SearchField("content", SearchFieldDataType.String) { IsSearchable = true });

        searchIndex.VectorSearch = new VectorSearch();
        searchIndex.VectorSearch.Algorithms.Add(new HnswAlgorithmConfiguration(VECTOR_ALG_NAME));
        searchIndex.VectorSearch.Profiles.Add(new VectorSearchProfile(VECTOR_PROFILE_NAME, VECTOR_ALG_NAME));

        await GetSearchIndexClient().CreateOrUpdateIndexAsync(searchIndex);
    }

    private SearchClient GetSearchClient(string collectionName)
    {
        if (!searchClients.TryGetValue(collectionName, out var searchClient))
        {
            AzureKeyCredential keyCredentials = new(config.AzureSearchKey);
            searchClient = new SearchClient(new Uri(this.config.AzureSearchEndpoint), collectionName, keyCredentials);
            searchClients.TryAdd(collectionName, searchClient);
        }

        return searchClient;
    }

    public async Task<(int SuccessCount, int ErrorCount)> ProcessAsync(IEnumerable<SearchModelDto> models, CancellationToken cancellationToken, string? collectionName = null)
    {
        var batch = IndexDocumentsBatch.Upload(models.Select(x => (SearchModel)x));
        var response = await GetSearchClient(collectionName ?? config.CollectionName).IndexDocumentsAsync(batch, cancellationToken: cancellationToken);

        int error = 0;
        int success = 0;
        foreach (var r in response.Value.Results)
        {
            if (r.Succeeded)
            {
                success++;
            }
            else
            {
                error++;
                logger.LogError("Failed to index document: {documentIndexError}", r.ErrorMessage);
            }
        }
        return (success, error);
    }

    public async Task<IEnumerable<IndexedDocument>> SearchAsync(string[] searchTexts, CancellationToken cancellationToken)
    {
        var embedding = embeddingList.GetSelectedEmbedding(config);
        var embeddings = (await embedding.GetEmbeddingsAsync(searchTexts, cancellationToken));

        var vectorSearchOptions = new VectorSearchOptions();
        foreach (var embed in embeddings)
        {
            var query = new VectorizedQuery(embed);
            query.Fields.Add("contentVector");
            query.KNearestNeighborsCount = 5;
            vectorSearchOptions.Queries.Add(query);
        }

        var searchOptions = new SearchOptions
        {
            VectorSearch = vectorSearchOptions
        };

        var results = await GetSearchClient(config.CollectionName).SearchAsync<SearchModel>(searchOptions, cancellationToken);

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
