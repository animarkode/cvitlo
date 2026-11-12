using System.Net.Http;
using System.Net.Http.Headers;

namespace Cvitlo.Common.Testing.Http;

public static class HttpClientAuthExtensions
{
    public static HttpClient WithAuthToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }
}
