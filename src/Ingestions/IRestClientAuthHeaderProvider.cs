using System.Net.Http.Headers;

namespace chatbot2.Ingestions;

public interface IRestClientAuthHeaderProvider
{
    Task<AuthenticationHeaderValue> GetAuthorizationHeader();
}
