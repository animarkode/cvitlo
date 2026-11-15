using System.Net;
using Cvitlo.Resumes.Application.Features.Links.CreateLink;
using Cvitlo.Resumes.Application.Features.Links.DeleteLink;
using Cvitlo.Resumes.Application.Features.Links.GetLink;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Links.DeleteLink;

[Collection<CollectionResumes>]
public class DeleteLinkTests(WebApp app) : TestBase
{
    [Fact]
    public async Task DeleteLink_NoToken_ShouldReturnUnauthorized()
    {
        var httpResponse = await app.Client
            .DELETEAsync<DeleteLinkEndpoint, DeleteLinkRequest>(
                new DeleteLinkRequest(Guid.NewGuid(), Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteLink_ExistingItem_ShouldRemoveIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = new CreateLinkRequestFaker(resume.Id).Generate();

        var (_, linkResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateLinkEndpoint, CreateLinkRequest, CreateLinkResponse>(createRequest);

        var deleteResponse = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteLinkEndpoint, DeleteLinkRequest>(
                new DeleteLinkRequest(resume.Id, linkResponse.Id));

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (getResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetLinkEndpoint, GetLinkRequest, GetLinkResponse>(
                new GetLinkRequest(resume.Id, linkResponse.Id));

        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteLink_UnknownLink_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var deleteResponse = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteLinkEndpoint, DeleteLinkRequest>(
                new DeleteLinkRequest(resume.Id, Guid.NewGuid()));

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteLink_OtherUsersResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);

        var createRequest = new CreateLinkRequestFaker(resume.Id).Generate();

        var (_, linkResponse) = await app.Client
            .WithAuthToken(userAToken)
            .POSTAsync<CreateLinkEndpoint, CreateLinkRequest, CreateLinkResponse>(createRequest);

        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .DELETEAsync<DeleteLinkEndpoint, DeleteLinkRequest>(
                new DeleteLinkRequest(resume.Id, linkResponse.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteLink_OwnResumeWithAnotherUsersItem_ShouldReturnNotFoundAndKeepIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);
        var resumeA = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);
        var resumeB = await ResumesTestHelper.CreateResumeAsync(app.Client, userBToken, ct: ct);
        var item = await CreateLinkAsync(userAToken, resumeA.Id);

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .DELETEAsync<DeleteLinkEndpoint, DeleteLinkRequest>(
                new DeleteLinkRequest(resumeB.Id, item.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var (getResponse, _) = await app.Client
            .WithAuthToken(userAToken)
            .GETAsync<GetLinkEndpoint, GetLinkRequest, GetLinkResponse>(
                new GetLinkRequest(resumeA.Id, item.Id));

        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task<CreateLinkResponse> CreateLinkAsync(
        string accessToken,
        Guid resumeId,
        int seed = ResumesTestConstants.LinkSeed)
    {
        var (_, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateLinkEndpoint, CreateLinkRequest, CreateLinkResponse>(
                new CreateLinkRequestFaker(resumeId, seed).Generate());

        return response;
    }

    [Fact]
    public async Task DeleteLink_AlreadyDeleted_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var item = await CreateLinkAsync(accessToken, resume.Id);

        var first = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteLinkEndpoint, DeleteLinkRequest>(new DeleteLinkRequest(resume.Id, item.Id));

        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var second = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteLinkEndpoint, DeleteLinkRequest>(new DeleteLinkRequest(resume.Id, item.Id));

        second.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
