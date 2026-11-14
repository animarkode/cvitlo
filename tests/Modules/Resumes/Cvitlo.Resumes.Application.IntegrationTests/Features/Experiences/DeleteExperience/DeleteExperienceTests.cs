using System.Net;
using Cvitlo.Resumes.Application.Features.Experiences.CreateExperience;
using Cvitlo.Resumes.Application.Features.Experiences.DeleteExperience;
using Cvitlo.Resumes.Application.Features.Experiences.GetExperience;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Experiences.DeleteExperience;

[Collection<CollectionResumes>]
[Priority(33)]
public class DeleteExperienceTests(WebApp app) : TestBase
{
    [Fact]
    [Priority(0)]
    public async Task DeleteExperience_NoToken_ShouldReturnUnauthorized()
    {
        app.Client.DefaultRequestHeaders.Authorization = null;

        var httpResponse = await app.Client
            .DELETEAsync<DeleteExperienceEndpoint, DeleteExperienceRequest>(
                new DeleteExperienceRequest(Guid.NewGuid(), Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Priority(1)]
    public async Task DeleteExperience_ShouldRemoveExperience()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = new CreateExperienceRequestFaker(resume.Id).Generate();

        var (_, ehResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateExperienceEndpoint, CreateExperienceRequest,
                CreateExperienceResponse>(createRequest);

        var deleteResponse = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteExperienceEndpoint, DeleteExperienceRequest>(
                new DeleteExperienceRequest(resume.Id, ehResponse.Id));

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (getResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetExperienceEndpoint, GetExperienceRequest, GetExperienceResponse>(
                new GetExperienceRequest(resume.Id, ehResponse.Id));

        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Priority(2)]
    public async Task NonExistentExperience_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteExperienceEndpoint, DeleteExperienceRequest>(
                new DeleteExperienceRequest(resume.Id, Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Priority(3)]
    public async Task CrossUserDeleteExperience_ShouldReturnForbidden()
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

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .DELETEAsync<DeleteExperienceEndpoint, DeleteExperienceRequest>(
                new DeleteExperienceRequest(resume.Id, ehResponse.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Priority(4)]
    public async Task DeleteExperience_OwnResumeWithAnotherUsersItem_ShouldReturnNotFoundAndKeepIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);
        var resumeA = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);
        var resumeB = await ResumesTestHelper.CreateResumeAsync(app.Client, userBToken, ct: ct);
        var item = await CreateExperienceAsync(userAToken, resumeA.Id);

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .DELETEAsync<DeleteExperienceEndpoint, DeleteExperienceRequest>(
                new DeleteExperienceRequest(resumeB.Id, item.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var (getResponse, _) = await app.Client
            .WithAuthToken(userAToken)
            .GETAsync<GetExperienceEndpoint, GetExperienceRequest, GetExperienceResponse>(
                new GetExperienceRequest(resumeA.Id, item.Id));

        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Priority(5)]
    public async Task DeleteExperience_AlreadyDeleted_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var item = await CreateExperienceAsync(accessToken, resume.Id);

        var first = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteExperienceEndpoint, DeleteExperienceRequest>(new DeleteExperienceRequest(resume.Id, item.Id));

        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var second = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteExperienceEndpoint, DeleteExperienceRequest>(new DeleteExperienceRequest(resume.Id, item.Id));

        second.StatusCode.ShouldBe(HttpStatusCode.NotFound);
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
