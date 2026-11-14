using System.Net;
using System.Net.Http.Headers;
using FastEndpoints.Testing;
using Shouldly;
using static Cvitlo.Api.IntegrationTests.Features.RateLimiting.RateLimitingTestConstants;

namespace Cvitlo.Api.IntegrationTests.Features.Authentication;

// IP octet 191 reserved for this class.
[Collection<CollectionRateLimiting>]
[Priority(20)]
public class AuthenticationPipelineTests(WebApp app) : TestBase
{
    [Theory]
    [InlineData("not-a-jwt")]
    [InlineData("eyJhbGciOiJub25lIn0.eyJzdWIiOiJhdHRhY2tlciJ9.")]
    public async Task User_BearerTokenNotIssuedByKeycloak_ShouldReturnUnauthorized(string token)
    {
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestClientIpStartupFilter.HeaderName, "203.0.113.191");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.GetAsync(UserUri, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
