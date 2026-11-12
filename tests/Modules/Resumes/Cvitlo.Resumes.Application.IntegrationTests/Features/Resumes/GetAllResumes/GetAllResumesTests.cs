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
[Priority(3)]
public class GetAllResumesTests(WebApp app) : TestBase
{
    [Fact]
    [Priority(1)]
    public async Task GetAllResumes_ShouldReturnUserResumes()
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

}