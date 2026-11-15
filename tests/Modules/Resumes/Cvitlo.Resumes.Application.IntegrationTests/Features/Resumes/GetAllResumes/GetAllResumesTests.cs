using System.Net;
using Cvitlo.Resumes.Application.Features.Resumes.CreateResume;
using Cvitlo.Resumes.Application.Features.Resumes.GetAllResumes;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Resumes.GetAllResumes;

[Collection<CollectionResumes>]
public class GetAllResumesTests(WebApp app) : TestBase
{
    [Fact]
    public async Task GetAllResumes_UserWithResume_ShouldReturnUserResumes()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var createRequest = new CreateResumeRequest("en-us");

        await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateResumeEndpoint, CreateResumeRequest, CreateResumeResponse>(createRequest);

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetAllResumesEndpoint, GetAllResumesResponse>();

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Items.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetAllResumes_NoToken_ShouldReturnUnauthorized()
    {
        var (httpResponse, _) = await app.Client
            .GETAsync<GetAllResumesEndpoint, GetAllResumesResponse>();

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllResumes_TwoUsers_ShouldReturnOnlyTheCallersResumes()
    {
        var ct = TestContext.Current.CancellationToken;
        var tokenA = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var tokenB = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);
        var resumeA = await ResumesTestHelper.CreateResumeAsync(app.Client, tokenA, ct: ct);
        var resumeB = await ResumesTestHelper.CreateResumeAsync(app.Client, tokenB, ct: ct);

        var (_, listB) = await app.Client
            .WithAuthToken(tokenB)
            .GETAsync<GetAllResumesEndpoint, GetAllResumesResponse>();

        var ids = listB.Items.Select(r => r.Id).ToList();
        ids.ShouldContain(resumeB.Id);
        ids.ShouldNotContain(resumeA.Id);
    }

    [Fact]
    public async Task GetAllResumes_SeveralResumes_ShouldReturnNewestFirst()
    {
        var ct = TestContext.Current.CancellationToken;
        var token = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);
        var created = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            created.Add((await ResumesTestHelper.CreateResumeAsync(app.Client, token, ct: ct)).Id);
        }

        var (_, list) = await app.Client
            .WithAuthToken(token)
            .GETAsync<GetAllResumesEndpoint, GetAllResumesResponse>();

        var ids = list.Items.Select(r => r.Id).ToList();
        var positions = created.Select(id => ids.IndexOf(id)).ToList();
        positions.ShouldAllBe(p => p >= 0);
        positions.ShouldBe(positions.OrderDescending().ToList());
    }
}
