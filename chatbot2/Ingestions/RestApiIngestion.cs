using System.Text.Json;
using Azure.Storage.Blobs;
using chatbot2.Configuration;
using chatbot2.Ingestions;
using Microsoft.Extensions.Logging;

namespace chatbot2.Ingestions;

public class RestApiIngestion : IVectorDbIngestion
{
    private readonly RestClientConfig config;
    private readonly IRestClientAuthHeaderProvider restClientAuthHeaderProvider;
    private readonly HttpClient httpClient;

    private readonly ILogger<RestApiIngestion> logger;


    public RestApiIngestion(IConfig config, IRestClientAuthHeaderProvider restClientAuthHeaderProvider, HttpClient httpClient, IngestionReporter ingestionReporter, ILogger<RestApiIngestion> logger)
    {
        var restClientConfig = Environment.GetEnvironmentVariable("RestClientConfig") ?? throw new Exception("Missing RestClientConfigFilePath");

        string json;
        if (restClientConfig.StartsWith(Util.BlobPrefix))
        {
            var parts = restClientConfig.Substring(Util.BlobPrefix.Length).Split('\\');
            var connectionString = config.AzureStorageConnectionString;
            var blob = new BlobClient(connectionString, parts[0], parts[1]);
            using var sr = new StreamReader(blob.DownloadContent().Value.Content.ToStream());
            json = sr.ReadToEnd();
        }
        else
        {
            json = File.ReadAllText(restClientConfig);
        }
        RestClientConfig rcConfig = JsonSerializer.Deserialize<RestClientConfig>(json) ?? throw new Exception("Failed to deserialize RestClientConfig");

        this.config = rcConfig;
        this.restClientAuthHeaderProvider = restClientAuthHeaderProvider;
        this.httpClient = httpClient;
        this.logger = logger;
        this.httpClient.BaseAddress = new Uri(rcConfig.BaseUrl ?? throw new Exception("config url is invalid"));
    }

    public async Task<List<SearchModel>> LoadDataAsync(CancellationToken cancellationToken)
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

        List<SearchModel> results = [];
        string? continuationRoute = config.InitialRoute;

        this.httpClient.DefaultRequestHeaders.Authorization = await restClientAuthHeaderProvider.GetAuthorizationHeader();

        while (continuationRoute is not null)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return results;
            }

            var response = await httpClient.GetStringAsync(continuationRoute, cancellationToken);
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
                foreach (var record in records)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return results;
                    }

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

                    results.Add(Create(record, config.Mappings));
                }
            }
        }

        return results;
    }

    const string DictionaryToJsonCoversion = "DictionaryToJson";

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
                searchModel.ContentToVectorized = mappedValue;
            }
        }

        if (searchModel.ContentToVectorized is null)
        {
            throw new Exception("vectorContent is not configured");
        }
        return searchModel;
    }
}
