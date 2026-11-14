using System.Net;
using Cvitlo.Resumes.Application.Features.Experiences.CreateExperience;
using Cvitlo.Resumes.Application.Features.Experiences.GetAllExperiences;
using Cvitlo.Resumes.Application.Features.Experiences.GetExperience;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Experiences.GetExperience;

[Collection<CollectionResumes>]
[Priority(31)]
public class GetExperienceTests(WebApp app) : TestBase
{
    [Fact]
    [Priority(0)]
    public async Task GetExperience_NoToken_ShouldReturnUnauthorized()
    {
        var (httpResponse, _) = await app.Client
            .GETAsync<GetExperienceEndpoint, GetExperienceRequest, GetExperienceResponse>(
                new GetExperienceRequest(Guid.NewGuid(), Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Priority(1)]
    public async Task GetExperience_ShouldReturnCreatedExperience()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = new CreateExperienceRequestFaker(resume.Id).Generate();

        var (_, ehResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateExperienceEndpoint, CreateExperienceRequest,
                CreateExperienceResponse>(createRequest);

        var (httpResponse, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetExperienceEndpoint, GetExperienceRequest, GetExperienceResponse>(
                new GetExperienceRequest(resume.Id, ehResponse.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        getResponse.Id.ShouldBe(ehResponse.Id);
        getResponse.JobTitle.ShouldBe(createRequest.JobTitle);
        getResponse.Employer.ShouldBe(createRequest.Employer);
    }

    [Fact]
    [Priority(2)]
    public async Task GetAllExperiences_ShouldReturnExperiencesForResume()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var items = new CreateExperienceRequestFaker(resume.Id).Generate(2);

        await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateExperienceEndpoint, CreateExperienceRequest,
                CreateExperienceResponse>(items[0]);

        await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateExperienceEndpoint, CreateExperienceRequest,
                CreateExperienceResponse>(items[1]);

        var (httpResponse, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetAllExperiencesEndpoint, GetAllExperiencesRequest,
                GetAllExperiencesResponse>(
                new GetAllExperiencesRequest(resume.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        getResponse.Items.Count.ShouldBe(2);
    }

    [Fact]
    [Priority(3)]
    public async Task NonExistentExperience_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetExperienceEndpoint, GetExperienceRequest, GetExperienceResponse>(
                new GetExperienceRequest(resume.Id, Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Priority(4)]
    public async Task GetAllExperiencesForNonExistentResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetAllExperiencesEndpoint, GetAllExperiencesRequest,
                GetAllExperiencesResponse>(
                new GetAllExperiencesRequest(Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Priority(5)]
    public async Task GetAllExperiencesEmptyResume_ShouldReturnEmptyList()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var (httpResponse, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetAllExperiencesEndpoint, GetAllExperiencesRequest,
                GetAllExperiencesResponse>(
                new GetAllExperiencesRequest(resume.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        getResponse.Items.ShouldBeEmpty();
    }

    [Fact]
    [Priority(6)]
    public async Task CrossUserGetExperience_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);

        var createRequest = new CreateExperienceRequestFaker(resume.Id).Generate();

        var (_, ehResponse) = await app.Client
            .WithAuthToken(userAToken)
            .POSTAsync<CreateExperienceEndpoint, CreateExperienceRequest,
                CreateExperienceResponse>(createRequest);

        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(userBToken)
            .GETAsync<GetExperienceEndpoint, GetExperienceRequest, GetExperienceResponse>(
                new GetExperienceRequest(resume.Id, ehResponse.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Priority(7)]
    public async Task CrossUserGetAllExperiences_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);

        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(userBToken)
            .GETAsync<GetAllExperiencesEndpoint, GetAllExperiencesRequest,
                GetAllExperiencesResponse>(
                new GetAllExperiencesRequest(resume.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Priority(8)]
    public async Task GetExperience_OwnResumeWithAnotherUsersItem_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);
        var resumeA = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);
        var resumeB = await ResumesTestHelper.CreateResumeAsync(app.Client, userBToken, ct: ct);
        var item = await CreateExperienceAsync(userAToken, resumeA.Id);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(userBToken)
            .GETAsync<GetExperienceEndpoint, GetExperienceRequest, GetExperienceResponse>(
                new GetExperienceRequest(resumeB.Id, item.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Priority(9)]
    public async Task GetExperience_ExperienceOfTheCallersOtherResume_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resumeA = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var resumeA2 = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var item = await CreateExperienceAsync(accessToken, resumeA.Id);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetExperienceEndpoint, GetExperienceRequest, GetExperienceResponse>(
                new GetExperienceRequest(resumeA2.Id, item.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Priority(10)]
    public async Task GetAllExperiences_NoToken_ShouldReturnUnauthorized()
    {
        var (httpResponse, _) = await app.Client
            .GETAsync<GetAllExperiencesEndpoint, GetAllExperiencesRequest, GetAllExperiencesResponse>(
                new GetAllExperiencesRequest(Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<CreateExperienceResponse> CreateExperienceAsync(
        string accessToken,
        Guid resumeId,
        int seed = ResumesTestConstants.ExperienceSeed)
    {
        var (_, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateExperienceEndpoint, CreateExperienceRequest, CreateExperienceResponse>(
                new CreateExperienceRequestFaker(resumeId, seed).Generate());

        return response;
    }
}
