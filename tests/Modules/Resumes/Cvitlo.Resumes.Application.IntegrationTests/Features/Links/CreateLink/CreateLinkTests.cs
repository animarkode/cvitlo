using Cvitlo.Resumes.Application.Features.Links.GetAllLinks;
using System.Net;
using Cvitlo.Resumes.Application.Features.Links.CreateLink;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Links.CreateLink.Data;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Links.CreateLink;

[Collection<CollectionResumes>]
[Priority(70)]
public class CreateLinkTests(WebApp app) : TestBase
{
    [Theory]
    [ClassData(typeof(ValidLinkData))]
    [Priority(1)]
    public async Task CreateLink_ValidRequest_ShouldReturnTheCreatedItem(CreateLinkRequest request)
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = request with { ResumeId = resume.Id };

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateLinkEndpoint, CreateLinkRequest, CreateLinkResponse>(createRequest);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Id.ShouldNotBe(Guid.Empty);
        response.ResumeId.ShouldBe(resume.Id);
        response.Label.ShouldBe(request.Label);
        response.Uri.ShouldBe(request.Uri);
    }

    [Fact]
    [Priority(2)]
    public async Task CreateLink_UnknownResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var request = new CreateLinkRequestFaker(Guid.NewGuid()).Generate();

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateLinkEndpoint, CreateLinkRequest, CreateLinkResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [ClassData(typeof(InvalidLinkData))]
    [Priority(2)]
    public async Task CreateLink_InvalidField_ShouldReturnBadRequest(
        CreateLinkRequest invalidRequest,
        string expectedKey)
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        
        var createRequest = invalidRequest with { ResumeId = resume.Id };
        
        var (httpResponse, error) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateLinkEndpoint, CreateLinkRequest, ErrorResponse>(createRequest);
        
        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        error.Errors.Keys.ShouldBe([expectedKey]);
    }

    [Fact]
    [Priority(3)]
    public async Task CreateLink_NoToken_ShouldReturnUnauthorized()
    {
        var (httpResponse, _) = await app.Client
            .POSTAsync<CreateLinkEndpoint, CreateLinkRequest, CreateLinkResponse>(
                new CreateLinkRequestFaker(Guid.NewGuid()).Generate());

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Priority(4)]
    public async Task CreateLink_SeveralItems_ShouldNumberThemInCreationOrder()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var first = await CreateLinkAsync(accessToken, resume.Id, 71001);
        var second = await CreateLinkAsync(accessToken, resume.Id, 71002);
        var third = await CreateLinkAsync(accessToken, resume.Id, 71003);

        new[] { first.Order, second.Order, third.Order }.ShouldBe(new[] { 0, 1, 2 });

        var (_, all) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetAllLinksEndpoint, GetAllLinksRequest, GetAllLinksResponse>(
                new GetAllLinksRequest(resume.Id));

        all.Items.Select(i => i.Id).ShouldBe([first.Id, second.Id, third.Id]);
    }

    [Fact]
    [Priority(5)]
    public async Task CreateLink_UriOf2083Characters_ShouldReturnOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var uri = "https://a.example/" + new string('a', 2065);

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateLinkEndpoint, CreateLinkRequest, CreateLinkResponse>(
                new CreateLinkRequestFaker(resume.Id).Generate() with { Uri = uri });

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Uri.ShouldBe(uri);
    }

    [Fact]
    [Priority(6)]
    public async Task CreateLink_LabelOf100Characters_ShouldReturnOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateLinkEndpoint, CreateLinkRequest, CreateLinkResponse>(
                new CreateLinkRequestFaker(resume.Id).Generate() with { Label = new string('a', 100) });

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Label!.Length.ShouldBe(100);
    }

    [Fact]
    [Priority(7)]
    public async Task CreateLink_UppercaseScheme_ShouldReturnOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateLinkEndpoint, CreateLinkRequest, CreateLinkResponse>(
                new CreateLinkRequestFaker(resume.Id).Generate() with { Uri = "HTTPS://A.EXAMPLE" });

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
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
}
