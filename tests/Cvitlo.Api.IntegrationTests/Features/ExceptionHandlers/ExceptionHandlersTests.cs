using System.Net;
using System.Text.Json;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Api.IntegrationTests.Features.ExceptionHandlers;

// IP octets 194-198 reserved for this class: every request here is anonymous and spends the
// global anonymous budget of its client IP.
[Collection<CollectionRateLimiting>]
[Priority(19)]
public class ExceptionHandlersTests(WebApp app) : TestBase
{
    private async Task<(HttpResponseMessage Response, string Body)> ThrowAsync(string kind, string clientIp)
    {
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestClientIpStartupFilter.HeaderName, clientIp);

        var response = await client.GetAsync(
            $"{TestThrowingRoutesStartupFilter.PathPrefix}/{kind}", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        return (response, body);
    }

    [Fact]
    public async Task Request_ConcurrentUpdateConflict_ShouldReturnConflict()
    {
        var (response, body) = await ThrowAsync("concurrency", "203.0.113.194");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("status").GetInt32().ShouldBe(409);
        json.RootElement.GetProperty("title").GetString().ShouldBe("Database Error");
        json.RootElement.GetProperty("detail").GetString().ShouldBe("The resource was modified by another request.");
        body.ShouldNotContain(TestThrowingRoutesStartupFilter.SecretMessage);
    }

    [Fact]
    public async Task Request_DatabaseUpdateFails_ShouldReturnInternalServerErrorWithoutTheCause()
    {
        var (response, body) = await ThrowAsync("db-update", "203.0.113.195");

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("status").GetInt32().ShouldBe(500);
        json.RootElement.GetProperty("title").GetString().ShouldBe("Database Error");
        body.ShouldNotContain(TestThrowingRoutesStartupFilter.SecretMessage);
        body.ShouldNotContain("DbUpdateException");
    }

    [Fact]
    public async Task Request_ValidationExceptionThrown_ShouldReturnBadRequestWithErrorsPerProperty()
    {
        var (response, body) = await ThrowAsync("validation", "203.0.113.196");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("status").GetInt32().ShouldBe(400);
        json.RootElement.GetProperty("title").GetString().ShouldBe("Validation Failed");

        var errors = json.RootElement.GetProperty("errors");
        errors.GetProperty("Title").EnumerateArray().Select(error => error.GetString())
            .ShouldBe(["Title is required.", "Title is too long."]);
        errors.GetProperty("Summary").EnumerateArray().Select(error => error.GetString())
            .ShouldBe(["Summary is too long."]);
    }

    [Fact]
    public async Task Request_UnexpectedException_ShouldReturnInternalServerErrorWithTraceIdOnly()
    {
        var (response, body) = await ThrowAsync("unexpected", "203.0.113.197");

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("status").GetInt32().ShouldBe(500);
        json.RootElement.GetProperty("title").GetString().ShouldBe("Internal Server Error");
        json.RootElement.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        body.ShouldNotContain(TestThrowingRoutesStartupFilter.SecretMessage);
        body.ShouldNotContain("InvalidOperationException");
        body.ShouldNotContain("   at ");
    }

    [Fact]
    public async Task Request_OperationCancelled_ShouldReturnClientClosedRequestWithoutBody()
    {
        var (response, body) = await ThrowAsync("cancelled", "203.0.113.198");

        ((int)response.StatusCode).ShouldBe(499);
        body.ShouldBeEmpty();
    }
}
