using System.Text.Json;

namespace chatbot2.Ingestions;

public class RestApiIngestion : IVectorDbIngestion
{
    private readonly RestClientConfig config;
    private readonly IRestClientAuthHeaderProvider restClientAuthHeaderProvider;
    private readonly HttpClient httpClient;
    private readonly IEmbedding embedding;
    public RestApiIngestion(IEnumerable<IEmbedding> embeddings, IRestClientAuthHeaderProvider restClientAuthHeaderProvider, HttpClient httpClient)
    {
        embedding = embeddings.GetSelectedEmbedding();
        var config = JsonSerializer.Deserialize<RestClientConfig>(
            File.ReadAllText(Environment.GetEnvironmentVariable("RestClientConfigFilePath") ?? throw new Exception("Missing RestClientConfigFilePath")));

        if (config is null)
        {
            throw new Exception("Failed to deserialize RestClientConfig");
        }

        this.config = config;
        this.restClientAuthHeaderProvider = restClientAuthHeaderProvider;
        this.httpClient = httpClient;
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

            continuationRoute = ((JsonElement)page[config.ContinuationField]).Deserialize<string>();


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
                                var compareValue = ((JsonElement)record[condition.SourceField]).Deserialize<string>();
                                if (compareValue == condition.Value)
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

                    searchModels.Add(Create(record, config.Mappings));
                }
            }
        }
    }

    private static SearchModel Create(IDictionary<string, object> record, RestClientMapping[] mappings)
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

            if (mapping.Target == "id")
            {
                var ids = mapping.Sources.Select(src => ((JsonElement)record[src]).Deserialize<string>());
                searchModel.Id = string.Join("_", ids);
            }

            if (mapping.Target == "title")
            {
                var titles = mapping.Sources.Select(src => ((JsonElement)record[src]).Deserialize<string>());
                searchModel.Title = string.Join("\n", titles);
            }
        }
        return searchModel;
    }
}
