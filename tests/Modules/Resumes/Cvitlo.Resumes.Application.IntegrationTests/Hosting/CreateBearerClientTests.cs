using System.Net;
using Cvitlo.Resumes.Application.Features.Resumes.GetAllResumes;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Hosting;

[Collection<CollectionResumes>]
[Priority(0)]
public class CreateBearerClientTests(WebApp app) : TestBase
{
    [Fact]
    public async Task CreateBearerClient_ValidUser_ShouldReturnClientAcceptedByAuthenticatedEndpoint()
    {
        var ct = TestContext.Current.CancellationToken;

        using var client = await app.CreateBearerClientAsync(app.Users[0], ct);

        var (httpResponse, _) = await client.GETAsync<GetAllResumesEndpoint, GetAllResumesResponse>();

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateBearerClient_ValidUser_ShouldNotChangeSharedClient()
    {
        var ct = TestContext.Current.CancellationToken;

        using var client = await app.CreateBearerClientAsync(app.Users[0], ct);

        app.Client.DefaultRequestHeaders.Authorization.ShouldBeNull();
    }
}
