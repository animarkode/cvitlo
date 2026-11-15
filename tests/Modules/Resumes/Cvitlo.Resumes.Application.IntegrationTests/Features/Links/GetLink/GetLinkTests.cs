using System.Net;
using Cvitlo.Resumes.Application.Features.Links.CreateLink;
using Cvitlo.Resumes.Application.Features.Links.GetAllLinks;
using Cvitlo.Resumes.Application.Features.Links.GetLink;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Links.GetLink;

[Collection<CollectionResumes>]
public class GetLinkTests(WebApp app) : TestBase
{
    [Fact]
    public async Task GetLink_NoToken_ShouldReturnUnauthorized()
    {
        var (httpResponse, _) = await app.Client
            .GETAsync<GetLinkEndpoint, GetLinkRequest, GetLinkResponse>(
                new GetLinkRequest(Guid.NewGuid(), Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetLink_ExistingItem_ShouldReturnIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = new CreateLinkRequestFaker(resume.Id).Generate();

        var (_, linkResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateLinkEndpoint, CreateLinkRequest, CreateLinkResponse>(createRequest);

        var (httpResponse, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetLinkEndpoint, GetLinkRequest, GetLinkResponse>(
                new GetLinkRequest(resume.Id, linkResponse.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        getResponse.Id.ShouldBe(linkResponse.Id);
        getResponse.Label.ShouldBe(createRequest.Label);
        getResponse.Uri.ShouldBe(createRequest.Uri);
    }

    [Fact]
    public async Task GetAllLinks_ResumeWithItems_ShouldReturnThem()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var items = new CreateLinkRequestFaker(resume.Id).Generate(2);

        await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateLinkEndpoint, CreateLinkRequest, CreateLinkResponse>(items[0]);

        await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateLinkEndpoint, CreateLinkRequest, CreateLinkResponse>(items[1]);

        var (httpResponse, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetAllLinksEndpoint, GetAllLinksRequest, GetAllLinksResponse>(
                new GetAllLinksRequest(resume.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        getResponse.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetLink_UnknownLink_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetLinkEndpoint, GetLinkRequest, GetLinkResponse>(
                new GetLinkRequest(resume.Id, Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllLinks_UnknownResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetAllLinksEndpoint, GetAllLinksRequest, GetAllLinksResponse>(
                new GetAllLinksRequest(Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllLinks_ResumeWithoutItems_ShouldReturnEmptyList()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var (httpResponse, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetAllLinksEndpoint, GetAllLinksRequest, GetAllLinksResponse>(
                new GetAllLinksRequest(resume.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        getResponse.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetLink_OtherUsersResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);

        var createRequest = new CreateLinkRequestFaker(resume.Id).Generate();

        var (_, linkResponse) = await app.Client
            .WithAuthToken(userAToken)
            .POSTAsync<CreateLinkEndpoint, CreateLinkRequest, CreateLinkResponse>(createRequest);

        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(userBToken)
            .GETAsync<GetLinkEndpoint, GetLinkRequest, GetLinkResponse>(
                new GetLinkRequest(resume.Id, linkResponse.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllLinks_OtherUsersResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);

        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(userBToken)
            .GETAsync<GetAllLinksEndpoint, GetAllLinksRequest, GetAllLinksResponse>(
                new GetAllLinksRequest(resume.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetLink_OwnResumeWithAnotherUsersItem_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);
        var resumeA = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);
        var resumeB = await ResumesTestHelper.CreateResumeAsync(app.Client, userBToken, ct: ct);
        var item = await CreateLinkAsync(userAToken, resumeA.Id);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(userBToken)
            .GETAsync<GetLinkEndpoint, GetLinkRequest, GetLinkResponse>(
                new GetLinkRequest(resumeB.Id, item.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetLink_ItemOfTheCallersOtherResume_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resumeA = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var resumeA2 = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var item = await CreateLinkAsync(accessToken, resumeA.Id);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetLinkEndpoint, GetLinkRequest, GetLinkResponse>(
                new GetLinkRequest(resumeA2.Id, item.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
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
    public async Task GetAllLinks_NoToken_ShouldReturnUnauthorized()
    {
        var (httpResponse, _) = await app.Client
            .GETAsync<GetAllLinksEndpoint, GetAllLinksRequest, GetAllLinksResponse>(
                new GetAllLinksRequest(Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
