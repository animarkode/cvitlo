using System.Net;
using Cvitlo.Common.Testing.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints.Testing;
using Shouldly;
using static Cvitlo.Api.IntegrationTests.Features.RateLimiting.RateLimitingTestConstants;

namespace Cvitlo.Api.IntegrationTests.Features.Testing;

// IP octet 190 and RegisteredUsersData index 2 are reserved for this class.
[Collection<CollectionRateLimiting>]
[Priority(18)]
public class HttpClientAuthExtensionsTests(WebApp app) : TestBase
{
    [Fact]
    public async Task WithAuthToken_SharedClient_ShouldLeaveTheSharedClientAnonymous()
    {
        var user = RegisteredUsersData.Generate()[2];
        var tokens = await app.Keycloak.GetTokensAsync(user.Email, user.Password, TestContext.Current.CancellationToken);
        var token = tokens["access_token"]!.GetValue<string>();

        using var shared = app.CreateClient();
        shared.DefaultRequestHeaders.Add(TestClientIpStartupFilter.HeaderName, "203.0.113.190");

        using var authenticated = shared.WithAuthToken(token);
        using var withToken = await authenticated.GetAsync(UserUri, TestContext.Current.CancellationToken);
        withToken.StatusCode.ShouldBe(HttpStatusCode.OK);

        // A token left on the shared client is what lets a "no token" test pass for the wrong reason.
        shared.DefaultRequestHeaders.Authorization.ShouldBeNull();
        using var withoutToken = await shared.GetAsync(UserUri, TestContext.Current.CancellationToken);
        withoutToken.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
