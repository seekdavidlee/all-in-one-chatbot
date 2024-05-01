using chatbot2.Configuration;
using System.Net.Http.Headers;
using System.Text.Json;

namespace chatbot2.Ingestions;

public class CustomAuthProvider : IRestClientAuthHeaderProvider
{
    private readonly HttpClient httpClient;
    private readonly IConfig config;

    public CustomAuthProvider(HttpClient httpClient, IConfig config)
    {
        httpClient.BaseAddress = new Uri(Environment.GetEnvironmentVariable("CustomAuthProviderUrl") ?? throw new Exception("Missing CustomAuthProviderUrl"));
        this.httpClient = httpClient;
        this.config = config;
    }
    public async Task<AuthenticationHeaderValue> GetAuthorizationHeader()
    {
        var rawContent = this.config.CustomAuthProviderContent;
        var pairs = rawContent.Split(';').Select(x =>
        {
            var parts = x.Split('=');
            return new KeyValuePair<string?, string?>(parts[0], parts[1]);
        });
        var content = new FormUrlEncodedContent(pairs);

        var response = await httpClient.SendAsync(new HttpRequestMessage
        {
            Content = content,
            Method = HttpMethod.Post,
        });

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        if (json is null)
        {
            throw new Exception("Unable to read response content");
        }

        var dic = JsonSerializer.Deserialize<IDictionary<string, string>>(json);
        if (dic is null)
        {
            throw new Exception("Unable to deserialize response content");
        }
        var token = dic["access_token"] ?? throw new Exception("no access token found");

        // todo: cache token, verify expires

        return new AuthenticationHeaderValue("OAuth", token);
    }
}
