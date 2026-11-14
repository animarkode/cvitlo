using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Cvitlo.Common.Testing.Http;

public static class HttpClientAuthExtensions
{
    /// <summary>
    /// Returns a client that sends <paramref name="token"/> as a bearer token through <paramref name="client"/>.
    /// The given client is left unchanged, so a token never stays behind on a client that tests share.
    /// </summary>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "The returned client owns the handler and disposes it.")]
    public static HttpClient WithAuthToken(this HttpClient client, string token)
    {
        return new HttpClient(new BearerTokenHandler(client, token))
        {
            BaseAddress = client.BaseAddress,
            Timeout = client.Timeout
        };
    }

    private sealed class BearerTokenHandler(HttpClient inner, string token) : HttpMessageHandler
    {
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
            Justification = "The response owns the copy as its RequestMessage.")]
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // An HttpClient refuses a request message another client has already sent, so the inner
            // client gets a copy. The inner client still adds its own default headers and cookies.
            var copy = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Content = request.Content,
                Version = request.Version,
                VersionPolicy = request.VersionPolicy
            };

            foreach (var header in request.Headers)
            {
                copy.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            foreach (var option in request.Options)
            {
                copy.Options.TryAdd(option.Key, option.Value);
            }

            copy.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return inner.SendAsync(copy, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
    }
}
