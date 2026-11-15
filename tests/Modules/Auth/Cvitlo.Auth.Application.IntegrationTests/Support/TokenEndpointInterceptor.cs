namespace Cvitlo.Auth.Application.IntegrationTests.Support;

// Sits between the API and Keycloak on the back channel (refresh and logout), so a test can make Keycloak
// misbehave the way it does in production: slow, erroring, or answering after the browser has gone away.
// Tests in one collection run one at a time, so a test sets Hook and clears it in a finally.
public sealed class TokenEndpointInterceptor
{
    public Func<HttpResponseMessage, HttpResponseMessage>? Hook { get; set; }

    public Func<HttpRequestMessage, HttpResponseMessage?>? Replace { get; set; }

    public DelegatingHandler CreateHandler() => new Handler(this);

    private sealed class Handler(TokenEndpointInterceptor interceptor) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            // A null answer lets the request through, so a test can fail one kind of call only.
            if (interceptor.Replace?.Invoke(request) is { } replaced)
            {
                return replaced;
            }

            var response = await base.SendAsync(request, ct);
            return interceptor.Hook is { } hook ? hook(response) : response;
        }
    }
}
