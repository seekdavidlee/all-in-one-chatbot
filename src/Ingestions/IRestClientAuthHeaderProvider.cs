using System.Net.Http.Headers;

namespace AIOChatbot.Ingestions;

public interface IRestClientAuthHeaderProvider
{
    Task<AuthenticationHeaderValue> GetAuthorizationHeader();
}
