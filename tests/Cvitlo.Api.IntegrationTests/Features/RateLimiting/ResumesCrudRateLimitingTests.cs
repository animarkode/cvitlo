using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cvitlo.Common.Testing.Http;
using FastEndpoints.Testing;
using Shouldly;
using static Cvitlo.Api.IntegrationTests.Features.RateLimiting.RateLimitingTestConstants;

namespace Cvitlo.Api.IntegrationTests.Features.RateLimiting;

[Collection<CollectionRateLimiting>]
public class ResumesCrudRateLimitingTests(WebApp app) : TestBase
{
    // IP octets 160-169 reserved for this class. Every test signs in a fresh account: an exhausted
    // authenticated-default budget stays exhausted for the 60-second window. Its limit (2) is far below the
    // global one (30), so a 429 on the 3rd request can only come from authenticated-default.

    private HttpClient CreateClientWithIp(string ip)
    {
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestClientIpStartupFilter.HeaderName, ip);

        return client;
    }

    // Listing resumes is the cheapest authenticated Resumes call: it carries no ResumeOwnerPolicy,
    // so a signed-in caller always gets a deterministic 200 and the measurement isolates the limiter.
    private static Task<HttpResponseMessage> ListResumesAsync(HttpClient client)
    {
        return client.GetAsync(ResumesUri, TestContext.Current.CancellationToken);
    }

    private static async Task<HttpResponseMessage> ExhaustAuthenticatedDefaultBudgetAsync(HttpClient client)
    {
        for (var i = 0; i < AuthenticatedDefaultPermitLimit; i++)
        {
            var allowed = await ListResumesAsync(client);
            allowed.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        return await ListResumesAsync(client);
    }

    [Fact]
    public async Task ListResumes_BeyondAuthenticatedDefaultLimit_ShouldReturn429WithRetryAfterAndGenericBody()
    {
        var token = await app.SignInNewUserAsync(TestContext.Current.CancellationToken);

        const string clientIp = "203.0.113.161";
        using var client = CreateClientWithIp(clientIp).WithAuthToken(token);

        var rejected = await ExhaustAuthenticatedDefaultBudgetAsync(client);

        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        rejected.Headers.RetryAfter.ShouldNotBeNull();
        rejected.Headers.RetryAfter.Delta.ShouldNotBeNull();
        rejected.Headers.RetryAfter.Delta.Value.TotalSeconds.ShouldBeGreaterThan(0);
        rejected.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        var body = await rejected.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("title").GetString().ShouldBe(ExpectedTitle);
        json.RootElement.GetProperty("detail").GetString().ShouldBe(ExpectedDetail);
        json.RootElement.GetProperty("status").GetInt32().ShouldBe(429);

        body.ShouldNotContain(clientIp);
        body.ShouldNotContain("PermitLimit");
        body.ShouldNotContain("ip:");
        body.ShouldNotContain("user:");
    }

    [Fact]
    public async Task ListResumes_DifferentUserOnSameIp_ShouldUseItsOwnPartition()
    {
        var firstToken = await app.SignInNewUserAsync(TestContext.Current.CancellationToken);
        var secondToken = await app.SignInNewUserAsync(TestContext.Current.CancellationToken);

        const string sharedIp = "203.0.113.164";
        using var firstClient = CreateClientWithIp(sharedIp).WithAuthToken(firstToken);
        using var secondClient = CreateClientWithIp(sharedIp).WithAuthToken(secondToken);

        var rejected = await ExhaustAuthenticatedDefaultBudgetAsync(firstClient);
        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

        // Same address, different `sub` claim => its own partition. One account hammering the resume
        // builder must not lock out everyone else behind the same corporate or campus egress IP.
        var secondUserResponse = await ListResumesAsync(secondClient);
        secondUserResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetEducations_ResumeListBudgetSpent_ShouldReturn429()
    {
        var token = await app.SignInNewUserAsync(TestContext.Current.CancellationToken);

        using var client = CreateClientWithIp("203.0.113.166").WithAuthToken(token);

        var rejected = await ExhaustAuthenticatedDefaultBudgetAsync(client);
        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

        // A named policy is one limiter shared by every opted-in endpoint, so moving to a nested collection buys no
        // second allowance. The resume id is unowned: the limiter runs before UseAuthorization, so a 429 here
        // can only be the limiter (otherwise ResumeOwnerPolicy answers 403).
        var nested = await client.GetAsync(
            string.Format(CultureInfo.InvariantCulture, ResumeEducationsUriFormat, Guid.NewGuid()),
            TestContext.Current.CancellationToken);

        nested.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }
}
