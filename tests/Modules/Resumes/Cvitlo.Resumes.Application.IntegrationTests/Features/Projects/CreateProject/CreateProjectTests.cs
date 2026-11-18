using Cvitlo.Resumes.Application.Features.Projects.GetProject;
using Cvitlo.Resumes.Application.Features.Projects.GetAllProjects;
using System.Net;
using Cvitlo.Resumes.Application.Features.Projects.CreateProject;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Projects.CreateProject.Data;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Projects.CreateProject;

[Collection<CollectionResumes>]
public class CreateProjectTests(WebApp app) : TestBase
{
    [Theory]
    [ClassData(typeof(ValidProjectData))]
    public async Task CreateProject_ValidRequest_ShouldReturnTheCreatedItem(CreateProjectRequest request)
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = request with { ResumeId = resume.Id };

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateProjectEndpoint, CreateProjectRequest, CreateProjectResponse>(createRequest);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Id.ShouldNotBe(Guid.Empty);
        response.ResumeId.ShouldBe(resume.Id);
        response.Tagline.ShouldBe(request.Tagline);
        response.Name.ShouldBe(request.Name);
    }

    [Fact]
    public async Task CreateProject_UnknownResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var request = new CreateProjectRequestFaker(Guid.NewGuid()).Generate();

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateProjectEndpoint, CreateProjectRequest, CreateProjectResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [ClassData(typeof(InvalidProjectData))]
    public async Task CreateProject_InvalidField_ShouldReturnBadRequest(
        CreateProjectRequest invalidRequest,
        string expectedKey)
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = invalidRequest with { ResumeId = resume.Id };

        var (httpResponse, error) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateProjectEndpoint, CreateProjectRequest, ErrorResponse>(createRequest);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        error.Errors.Keys.ShouldBe([expectedKey]);
    }

    [Fact]
    public async Task CreateProject_NoToken_ShouldReturnUnauthorized()
    {
        var (httpResponse, _) = await app.Client
            .POSTAsync<CreateProjectEndpoint, CreateProjectRequest, CreateProjectResponse>(
                new CreateProjectRequestFaker(Guid.NewGuid()).Generate());

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProject_SeveralItems_ShouldNumberThemInCreationOrder()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var first = await CreateProjectAsync(accessToken, resume.Id, 71001);
        var second = await CreateProjectAsync(accessToken, resume.Id, 71002);
        var third = await CreateProjectAsync(accessToken, resume.Id, 71003);

        new[] { first.Order, second.Order, third.Order }.ShouldBe(new[] { 0, 1, 2 });

        var (_, all) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetAllProjectsEndpoint, GetAllProjectsRequest, GetAllProjectsResponse>(
                new GetAllProjectsRequest(resume.Id));

        all.Items.Select(i => i.Id).ShouldBe([first.Id, second.Id, third.Id]);
    }

    [Fact]
    public async Task CreateProject_UnsafeDescription_ShouldStoreSanitizedHtml()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        const string unsafeDescription =
            "<p><strong>Module</strong> on XSS</p>"
            + "<script>alert('xss')</script>"
            + "<img src=x onerror=\"alert(1)\" />";

        var request = new CreateProjectRequestFaker(resume.Id).Generate() with { Description = unsafeDescription };

        var (createResponse, created) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateProjectEndpoint, CreateProjectRequest, CreateProjectResponse>(request);

        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var (_, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetProjectEndpoint, GetProjectRequest, GetProjectResponse>(
                new GetProjectRequest(resume.Id, created.Id));

        getResponse.Description.ShouldNotBeNull();
        getResponse.Description.ShouldNotContain("<script");
        getResponse.Description.ShouldNotContain("onerror");
        getResponse.Description.ShouldContain("<strong>Module</strong>");
    }

    [Fact]
    public async Task CreateProject_DescriptionOf1000VisibleCharactersInAParagraph_ShouldReturnBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var request = new CreateProjectRequestFaker(resume.Id).Generate() with
        {
            Description = "<p>" + new string('a', 1000) + "</p>"
        };

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateProjectEndpoint, CreateProjectRequest, ErrorResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProject_DescriptionOfExactly1000CharactersThatDoesNotGrow_ShouldReturnOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var request = new CreateProjectRequestFaker(resume.Id).Generate() with { Description = new string('a', 1000) };

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateProjectEndpoint, CreateProjectRequest, CreateProjectResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task<CreateProjectResponse> CreateProjectAsync(
        string accessToken,
        Guid resumeId,
        int seed = ResumesTestConstants.CourseSeed)
    {
        var (_, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateProjectEndpoint, CreateProjectRequest, CreateProjectResponse>(
                new CreateProjectRequestFaker(resumeId, seed).Generate());

        return response;
    }
}
