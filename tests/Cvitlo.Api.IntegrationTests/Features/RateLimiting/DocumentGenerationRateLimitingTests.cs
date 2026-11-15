using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cvitlo.Resumes.Application.Features.Resumes.CreateResume;
using Cvitlo.Common.Testing.Http;
using FastEndpoints.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using static Cvitlo.Api.IntegrationTests.Features.RateLimiting.RateLimitingTestConstants;

namespace Cvitlo.Api.IntegrationTests.Features.RateLimiting;

[Collection<CollectionRateLimiting>]
public class DocumentGenerationRateLimitingTests(WebApp app) : TestBase
{
    // IP octets 170-179 reserved for this class; each test signs in a fresh account (one resume at most).
    // The fixture pins expensive-resource to one permit and no queue; holding the gate parks a render in the
    // handler with that permit taken, so the next document request is refused deterministically.

    private HttpClient CreateClientWithIp(string ip)
    {
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestClientIpStartupFilter.HeaderName, ip);

        return client;
    }

    private GatedDocumentsModuleApi Documents => app.Services.GetRequiredService<GatedDocumentsModuleApi>();

    private static async Task<Guid> CreateResumeAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            ResumesUri,
            new CreateResumeRequest("en-us"),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var created = await response.Content.ReadFromJsonAsync<CreateResumeResponse>(
            TestContext.Current.CancellationToken);

        created.ShouldNotBeNull();

        return created.Id;
    }

    // Empty JSON object like the Angular client: without a JSON Content-Type FastEndpoints answers 415,
    // and a literal `null` body has none, which silently starves Documents.Entered.
    private static Task<HttpResponseMessage> GenerateAsync(HttpClient client, Guid resumeId)
    {
        return client.PostAsJsonAsync(
            string.Format(CultureInfo.InvariantCulture, ResumeGenerateUriFormat, resumeId),
            new { },
            TestContext.Current.CancellationToken);
    }

    private static Task<HttpResponseMessage> DownloadAsync(HttpClient client, Guid resumeId)
    {
        return client.GetAsync(
            string.Format(CultureInfo.InvariantCulture, ResumeDownloadUriFormat, resumeId),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateDocument_AnotherRenderInProgress_ShouldReturn429WithRetryAfterAndGenericBody()
    {
        var token = await app.SignInNewUserAsync(TestContext.Current.CancellationToken);

        const string clientIp = "203.0.113.171";
        using var client = CreateClientWithIp(clientIp).WithAuthToken(token);

        var resumeId = await CreateResumeAsync(client);

        using var gate = Documents.Hold();

        var parked = GenerateAsync(client, resumeId);
        // Bounded so a request that never reaches the renderer fails after 30s instead of hanging the suite.
        await Documents.Entered.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        var rejected = await GenerateAsync(client, resumeId);

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

        gate.Dispose();

        var released = await parked;
        released.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DownloadDocument_RenderInProgress_ShouldReturn429()
    {
        var token = await app.SignInNewUserAsync(TestContext.Current.CancellationToken);

        using var client = CreateClientWithIp("203.0.113.173").WithAuthToken(token);

        var resumeId = await CreateResumeAsync(client);

        using var gate = Documents.Hold();

        var parked = GenerateAsync(client, resumeId);
        // Bounded so a request that never reaches the renderer fails after 30s instead of hanging the suite.
        await Documents.Entered.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        // Generate and download run the same renderer, so they must draw on one budget — switching
        // endpoints cannot buy a second simultaneous render.
        var rejected = await DownloadAsync(client, resumeId);
        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

        gate.Dispose();

        var released = await parked;
        released.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GenerateDocument_AuthenticatedDefaultBudgetSpent_ShouldReturnOk()
    {
        var token = await app.SignInNewUserAsync(TestContext.Current.CancellationToken);

        using var crudClient = CreateClientWithIp("203.0.113.175").WithAuthToken(token);

        var resumeId = await CreateResumeAsync(crudClient);

        // Creating the resume already spent one authenticated-default permit; spend the rest.
        for (var i = 1; i < AuthenticatedDefaultPermitLimit; i++)
        {
            var allowed = await crudClient.GetAsync(ResumesUri, TestContext.Current.CancellationToken);
            allowed.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        var exhausted = await crudClient.GetAsync(ResumesUri, TestContext.Current.CancellationToken);
        exhausted.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

        // Document rendering has its own limiter, so a CRUD burst must never leave the same account
        // unable to export the resume it just finished editing.
        using var documentClient = CreateClientWithIp("203.0.113.176").WithAuthToken(token);

        var generated = await GenerateAsync(documentClient, resumeId);
        generated.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
