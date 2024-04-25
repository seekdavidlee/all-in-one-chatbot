using System.Text.Json;
using chatbot2.Ingestions;
using Microsoft.Extensions.Logging;

namespace chatbot2.Ingestions;

public class RestApiIngestion : IVectorDbIngestion
{
    private readonly RestClientConfig config;
    private readonly IRestClientAuthHeaderProvider restClientAuthHeaderProvider;
    private readonly HttpClient httpClient;
    private readonly ILogger<RestApiIngestion> logger;
    private readonly IEmbedding embedding;
    private readonly int BatchSize = 30;
    public RestApiIngestion(IEnumerable<IEmbedding> embeddings, IRestClientAuthHeaderProvider restClientAuthHeaderProvider, HttpClient httpClient, ILogger<RestApiIngestion> logger)
    {
        embedding = embeddings.GetSelectedEmbedding();
        var config = JsonSerializer.Deserialize<RestClientConfig>(
            File.ReadAllText(Environment.GetEnvironmentVariable("RestClientConfigFilePath") ?? throw new Exception("Missing RestClientConfigFilePath")));

        if (config is null)
        {
            throw new Exception("Failed to deserialize RestClientConfig");
        }

        var ingestionBatchSize = Environment.GetEnvironmentVariable("IngestionBatchSize");
        if (ingestionBatchSize is not null)
        {
            if (int.TryParse(ingestionBatchSize, out int batchSize))
            {
                BatchSize = batchSize;
            }
        }

        this.config = config;
        this.restClientAuthHeaderProvider = restClientAuthHeaderProvider;
        this.httpClient = httpClient;
        this.logger = logger;
        this.httpClient.BaseAddress = new Uri(config.BaseUrl ?? throw new Exception("config url is invalid"));
    }

    public async Task RunAsync(IVectorDb vectorDb)
    {
        if (config.Mappings is null)
        {
            throw new Exception("config Mappings is invalid");
        }

        if (config.RecordsField is null)
        {
            throw new Exception("config RecordsField is invalid");
        }

        if (config.ContinuationField is null)
        {
            throw new Exception("config ContinuationField is invalid");
        }

        if (config.InitialRoute is null)
        {
            throw new Exception("config InitialRoute is invalid");
        }
        string? continuationRoute = config.InitialRoute;

        this.httpClient.DefaultRequestHeaders.Authorization = await restClientAuthHeaderProvider.GetAuthorizationHeader();

        while (continuationRoute is not null)
        {
            var response = await this.httpClient.GetStringAsync(continuationRoute);
            if (response is null)
            {
                throw new Exception("Unable to read response content");
            }

            var page = JsonSerializer.Deserialize<IDictionary<string, object>>(response);
            if (page is null)
            {
                throw new Exception("Failed to deserialize response content");
            }

            logger.LogInformation("Processing page {continuationRoute}", continuationRoute);

            continuationRoute = page.GetValue(config.ContinuationField)?.Value;

            var records = ((JsonElement)page[config.RecordsField]).Deserialize<IDictionary<string, object>[]>();
            if (records is not null)
            {
                List<SearchModel> searchModels = [];
                foreach (var record in records)
                {
                    bool shouldAdd = true;
                    if (config.AndConditions is not null)
                    {
                        foreach (var condition in config.AndConditions)
                        {
                            if (condition.SourceField is not null && condition.Operator == "equal")
                            {
                                var compareValue = record.GetValue(condition.SourceField)?.Value;
                                if (compareValue == condition.Value)
                                {
                                    shouldAdd = false;
                                    break;
                                }
                            }

                            if (condition.SourceField is not null && condition.Operator == "isNotNull")
                            {
                                var evalRec = record.GetValue(condition.SourceField);
                                if (evalRec is null || evalRec.Value.Value is null)
                                {
                                    shouldAdd = false;
                                    break;
                                }
                            }
                        }
                    }
                    if (!shouldAdd)
                    {
                        continue;
                    }

                    searchModels.Add(await CreateAsync(embedding, record, config.Mappings));

                    if (searchModels.Count >= BatchSize)
                    {
                        logger.LogInformation("Uploading batch of {0} records", searchModels.Count);
                        await vectorDb.ProcessAsync(searchModels);
                        searchModels.Clear();
                    }
                }

                if (searchModels.Count > 0)
                {
                    logger.LogInformation("Uploading batch of {0} records", searchModels.Count);
                    await vectorDb.ProcessAsync(searchModels);
                    searchModels.Clear();
                }
            }
        }
    }

    const string DictionaryToJsonCoversion = "DictionaryToJson";

    private static async Task<SearchModel> CreateAsync(IEmbedding embedding, IDictionary<string, object> record, RestClientMapping[] mappings)
    {
        var searchModel = new SearchModel
        {
            Id = Guid.NewGuid().ToString(),
        };
        foreach (var mapping in mappings)
        {
            if (mapping.Sources is null)
            {
                continue;
            }

            var items = mapping.Sources.Select(record.GetValue).Where(x => x is not null && x.Value.Value is not null);

            string mappedValue;
            if (mapping.Conversion == DictionaryToJsonCoversion)
            {
                var dic = new Dictionary<string, string>();
                foreach (var item in items)
                {
                    dic[item?.Key ?? throw new Exception("key is invalid")] = item?.Value ?? throw new Exception("value is invalid");
                }

                if (mapping.Tags is not null)
                {
                    foreach (var tag in mapping.Tags)
                    {
                        dic[tag.Key] = tag.Value;
                    }
                }
                mappedValue = JsonSerializer.Serialize(dic);
            }
            else
            {
                mappedValue = string.Join(mapping.SourcesJoinChar, items.Select(x => x?.Value));
            }

            if (mapping.Target == "id")
            {
                searchModel.Id = mappedValue;
            }

            if (mapping.Target == "title")
            {

                searchModel.Title = mappedValue;
            }

            if (mapping.Target == "content")
            {
                searchModel.Content = mappedValue;
            }

            if (mapping.Target == "url")
            {
                searchModel.Url = mappedValue;
            }

            if (mapping.Target == "meta_json_string")
            {
                searchModel.MetaData = mappedValue;
            }

            if (mapping.Target == "contentVector")
            {
                searchModel.ContentVector = await embedding.GetEmbeddingsAsync(mappedValue);
            }
        }
        return searchModel;
    }
}
