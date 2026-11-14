using System.Net;
using Cvitlo.Resumes.Application.Features.Educations.CreateEducation;
using Cvitlo.Resumes.Application.Features.Educations.DeleteEducation;
using Cvitlo.Resumes.Application.Features.Educations.GetEducation;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Educations.DeleteEducation;

[Collection<CollectionResumes>]
[Priority(23)]
public class DeleteEducationTests(WebApp app) : TestBase
{
    [Fact]
    [Priority(0)]
    public async Task DeleteEducation_NoToken_ShouldReturnUnauthorized()
    {
        var httpResponse = await app.Client
            .DELETEAsync<DeleteEducationEndpoint, DeleteEducationRequest>(
                new DeleteEducationRequest(Guid.NewGuid(), Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Priority(1)]
    public async Task DeleteEducation_ExistingItem_ShouldRemoveIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = new CreateEducationRequestFaker(resume.Id).Generate();

        var (_, educationResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateEducationEndpoint, CreateEducationRequest, CreateEducationResponse>(createRequest);

        var deleteResponse = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteEducationEndpoint, DeleteEducationRequest>(
                new DeleteEducationRequest(resume.Id, educationResponse.Id));

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (getResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetEducationEndpoint, GetEducationRequest, GetEducationResponse>(
                new GetEducationRequest(resume.Id, educationResponse.Id));

        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Priority(2)]
    public async Task DeleteEducation_UnknownEducation_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteEducationEndpoint, DeleteEducationRequest>(
                new DeleteEducationRequest(resume.Id, Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Priority(3)]
    public async Task DeleteEducation_OtherUsersResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);

        var createRequest = new CreateEducationRequestFaker(resume.Id).Generate();

        var (_, educationResponse) = await app.Client
            .WithAuthToken(userAToken)
            .POSTAsync<CreateEducationEndpoint, CreateEducationRequest, CreateEducationResponse>(createRequest);

        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .DELETEAsync<DeleteEducationEndpoint, DeleteEducationRequest>(
                new DeleteEducationRequest(resume.Id, educationResponse.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Priority(4)]
    public async Task DeleteEducation_OwnResumeWithAnotherUsersItem_ShouldReturnNotFoundAndKeepIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);
        var resumeA = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);
        var resumeB = await ResumesTestHelper.CreateResumeAsync(app.Client, userBToken, ct: ct);
        var item = await CreateEducationAsync(userAToken, resumeA.Id);

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .DELETEAsync<DeleteEducationEndpoint, DeleteEducationRequest>(
                new DeleteEducationRequest(resumeB.Id, item.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var (getResponse, _) = await app.Client
            .WithAuthToken(userAToken)
            .GETAsync<GetEducationEndpoint, GetEducationRequest, GetEducationResponse>(
                new GetEducationRequest(resumeA.Id, item.Id));

        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Priority(5)]
    public async Task DeleteEducation_AlreadyDeleted_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var item = await CreateEducationAsync(accessToken, resume.Id);

        var first = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteEducationEndpoint, DeleteEducationRequest>(new DeleteEducationRequest(resume.Id, item.Id));

        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var second = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteEducationEndpoint, DeleteEducationRequest>(new DeleteEducationRequest(resume.Id, item.Id));

        second.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<CreateEducationResponse> CreateEducationAsync(
        string accessToken,
        Guid resumeId,
        int seed = ResumesTestConstants.EducationSeed)
    {
        var (_, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateEducationEndpoint, CreateEducationRequest, CreateEducationResponse>(
                new CreateEducationRequestFaker(resumeId, seed).Generate());

        return response;
    }
}
