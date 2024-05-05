namespace AIOChatbot.Ingestions;

public class RestClientConfig
{
    public string? BaseUrl { get; set; }
    public string? InitialRoute { get; set; }
    public string? ContinuationField { get; set; }

    public string? RecordsField { get; set; }

    public RestClientMapping[]? Mappings { get; set; }
    public RestClientMappingCondition[]? AndConditions { get; set; }
}
