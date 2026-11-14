using System.Net;
using Cvitlo.Common.Testing.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints.Testing;
using Shouldly;
using static Cvitlo.Api.IntegrationTests.Features.RateLimiting.RateLimitingTestConstants;

namespace Cvitlo.Api.IntegrationTests.Features.RateLimiting;

// IP octets 180-185 and RegisteredUsersData indices 3-4 are reserved for this class.
[Collection<CollectionRateLimiting>]
[Priority(17)]
public class AuthEndpointsRateLimitingTests(WebApp app) : TestBase
{
    private HttpClient CreateClientWithIp(string ip)
    {
        var client = app.CreateClient(new ClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestClientIpStartupFilter.HeaderName, ip);

        return client;
    }

    private async Task<string> SignInAsync(int userIndex)
    {
        var user = RegisteredUsersData.Generate()[userIndex];
        var tokens = await app.Keycloak.GetTokensAsync(user.Email, user.Password, TestContext.Current.CancellationToken);

        return tokens["access_token"]!.GetValue<string>();
    }

    private static async Task<HttpResponseMessage> PostBackchannelLogoutAsync(HttpClient client)
    {
        using var form = new FormUrlEncodedContent(
            new Dictionary<string, string>(StringComparer.Ordinal) { ["logout_token"] = "forged" });

        return await client.PostAsync(BackchannelLogoutUri, form, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Login_BeyondAuthExternalLimit_ShouldReturn429()
    {
        using var client = CreateClientWithIp("203.0.113.180");

        for (var i = 0; i < AuthExternalPermitLimit; i++)
        {
            using var allowed = await client.GetAsync(LoginUri, TestContext.Current.CancellationToken);
            allowed.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        }

        using var rejected = await client.GetAsync(LoginUri, TestContext.Current.CancellationToken);
        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Register_BeyondAuthExternalLimit_ShouldReturn429()
    {
        using var client = CreateClientWithIp("203.0.113.185");

        for (var i = 0; i < AuthExternalPermitLimit; i++)
        {
            using var allowed = await client.GetAsync(RegisterUri, TestContext.Current.CancellationToken);
            allowed.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        }

        using var rejected = await client.GetAsync(RegisterUri, TestContext.Current.CancellationToken);
        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Logout_BeyondAuthSessionLimit_ShouldReturn429()
    {
        using var client = CreateClientWithIp("203.0.113.182");

        for (var i = 0; i < AuthSessionPermitLimit; i++)
        {
            using var allowed = await client.PostAsync(LogoutUri, null, TestContext.Current.CancellationToken);
            allowed.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        using var rejected = await client.PostAsync(LogoutUri, null, TestContext.Current.CancellationToken);
        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task BackchannelLogout_BeyondAuthSessionLimit_ShouldReturn429()
    {
        using var client = CreateClientWithIp("203.0.113.181");

        for (var i = 0; i < AuthSessionPermitLimit; i++)
        {
            using var allowed = await PostBackchannelLogoutAsync(client);
            allowed.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        using var rejected = await PostBackchannelLogoutAsync(client);
        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    // A per-user partition on an anonymous endpoint would hand a caller a second budget for the
    // price of adding a bearer token, so these budgets must stay keyed on the client IP.
    [Fact]
    public async Task Login_IpBudgetSpentThenBearerTokenAdded_ShouldReturn429()
    {
        var token = await SignInAsync(3);
        using var client = CreateClientWithIp("203.0.113.183");

        for (var i = 0; i < AuthExternalPermitLimit; i++)
        {
            using var allowed = await client.GetAsync(LoginUri, TestContext.Current.CancellationToken);
            allowed.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        }

        using var withToken = client.WithAuthToken(token);
        using var rejected = await withToken.GetAsync(LoginUri, TestContext.Current.CancellationToken);
        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Logout_IpBudgetSpentThenBearerTokenAdded_ShouldReturn429()
    {
        var token = await SignInAsync(4);
        using var client = CreateClientWithIp("203.0.113.184");

        for (var i = 0; i < AuthSessionPermitLimit; i++)
        {
            using var allowed = await client.PostAsync(LogoutUri, null, TestContext.Current.CancellationToken);
            allowed.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        using var withToken = client.WithAuthToken(token);
        using var rejected = await withToken.PostAsync(LogoutUri, null, TestContext.Current.CancellationToken);
        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }
}
