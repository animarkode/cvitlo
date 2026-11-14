using System.Net;
using Cvitlo.Resumes.Application.Features.Projects.CreateProject;
using Cvitlo.Resumes.Application.Features.Projects.GetProject;
using Cvitlo.Resumes.Application.Features.Projects.UpdateProject;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Projects.UpdateProject.Data;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Projects.UpdateProject;

[Collection<CollectionResumes>]
[Priority(62)]
public class UpdateProjectTests(WebApp app) : TestBase
{
    [Fact]
    [Priority(0)]
    public async Task UpdateProject_NoToken_ShouldReturnUnauthorized()
    {
        var httpResponse = await app.Client
            .PUTAsync<UpdateProjectEndpoint, UpdateProjectRequest>(
                new UpdateProjectRequest(Guid.NewGuid(), Guid.NewGuid(), "Tagline", "Project", null, null, null, null));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Priority(1)]
    public async Task UpdateProject_ValidRequest_ShouldModifyFields()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = new CreateProjectRequestFaker(resume.Id).Generate();

        var (_, projectResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateProjectEndpoint, CreateProjectRequest, CreateProjectResponse>(createRequest);

        var updated = new CreateProjectRequestFaker(resume.Id, 62001).Generate();

        var updateRequest = new UpdateProjectRequest(
            projectResponse.Id,
            resume.Id,
            updated.Tagline,
            updated.Name,
            updated.Url,
            updated.StartDate,
            updated.EndDate,
            updated.Description);

        var updateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateProjectEndpoint, UpdateProjectRequest>(updateRequest);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (_, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetProjectEndpoint, GetProjectRequest, GetProjectResponse>(
                new GetProjectRequest(resume.Id, projectResponse.Id));

        getResponse.Tagline.ShouldBe(updated.Tagline);
        getResponse.Name.ShouldBe(updated.Name);
    }

    [Fact]
    [Priority(2)]
    public async Task UpdateProject_UnknownResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var project = new CreateProjectRequestFaker(Guid.NewGuid()).Generate();

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateProjectEndpoint, UpdateProjectRequest>(
                new UpdateProjectRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    project.Tagline,
                    project.Name,
                    project.Url,
                    project.StartDate,
                    project.EndDate,
                    project.Description));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Priority(3)]
    public async Task UpdateProject_OtherUsersResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);

        var createRequest = new CreateProjectRequestFaker(resume.Id).Generate();

        var (_, projectResponse) = await app.Client
            .WithAuthToken(userAToken)
            .POSTAsync<CreateProjectEndpoint, CreateProjectRequest, CreateProjectResponse>(createRequest);

        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);

        var updated = new CreateProjectRequestFaker(resume.Id, 62002).Generate();

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .PUTAsync<UpdateProjectEndpoint, UpdateProjectRequest>(
                new UpdateProjectRequest(
                    projectResponse.Id,
                    resume.Id,
                    updated.Tagline,
                    updated.Name,
            updated.Url,
                    updated.StartDate,
                    updated.EndDate,
                    updated.Description));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [ClassData(typeof(InvalidUpdateProjectData))]
    [Priority(4)]
    public async Task UpdateProject_InvalidField_ShouldReturnBadRequest(
        UpdateProjectRequest invalidRequest,
        string expectedKey)
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = new CreateProjectRequestFaker(resume.Id).Generate();

        var (_, projectResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateProjectEndpoint, CreateProjectRequest, CreateProjectResponse>(createRequest);

        var updateRequest = invalidRequest with { Id = projectResponse.Id, ResumeId = resume.Id };

        var (httpResponse, error) = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateProjectEndpoint, UpdateProjectRequest, ErrorResponse>(updateRequest);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        error.Errors.Keys.ShouldBe([expectedKey]);
    }

    [Fact]
    [Priority(5)]
    public async Task UpdateProject_WithUnsafeDescription_ShouldPersistSanitizedHtml()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = new CreateProjectRequestFaker(resume.Id).Generate();

        var (_, projectResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateProjectEndpoint, CreateProjectRequest, CreateProjectResponse>(createRequest);

        const string unsafeDescription =
            "<p><strong>Module</strong> on XSS</p>"
            + "<script>alert('xss')</script>"
            + "<img src=x onerror=\"alert(1)\" />";

        var updateRequest = new UpdateProjectRequest(
            projectResponse.Id, resume.Id,
            "Udemy", "Security 101", null, null, null, unsafeDescription);

        var updateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateProjectEndpoint, UpdateProjectRequest>(updateRequest);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (_, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetProjectEndpoint, GetProjectRequest, GetProjectResponse>(
                new GetProjectRequest(resume.Id, projectResponse.Id));

        getResponse.Description.ShouldNotBeNull();
        getResponse.Description.ShouldNotContain("<script");
        getResponse.Description.ShouldNotContain("onerror");
        getResponse.Description.ShouldContain("<strong>Module</strong>");
    }

    [Fact]
    [Priority(6)]
    public async Task UpdateProject_OwnResumeWithAnotherUsersItem_ShouldReturnNotFoundAndChangeNothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);
        var resumeA = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);
        var resumeB = await ResumesTestHelper.CreateResumeAsync(app.Client, userBToken, ct: ct);
        var item = await CreateProjectAsync(userAToken, resumeA.Id);
        var changed = new CreateProjectRequestFaker(resumeB.Id, 72901).Generate();

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .PUTAsync<UpdateProjectEndpoint, UpdateProjectRequest>(
                new UpdateProjectRequest(item.Id, resumeB.Id, changed.Tagline, changed.Name, changed.Url, changed.StartDate, changed.EndDate, changed.Description));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var (_, getResponse) = await app.Client
            .WithAuthToken(userAToken)
            .GETAsync<GetProjectEndpoint, GetProjectRequest, GetProjectResponse>(
                new GetProjectRequest(resumeA.Id, item.Id));

        getResponse.Name.ShouldBe(item.Name);
    }

    [Fact]
    [Priority(7)]
    public async Task UpdateProject_UnknownProject_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var project = new CreateProjectRequestFaker(resume.Id).Generate();

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateProjectEndpoint, UpdateProjectRequest>(
                new UpdateProjectRequest(
                    Guid.NewGuid(),
                    resume.Id,
                    project.Tagline,
                    project.Name,
                    project.Url,
                    project.StartDate,
                    project.EndDate,
                    project.Description));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(Skip = "#147: <p> + 1000 chars + </p> passes the visible-length rule but the stored HTML overflows varchar(1000); answers 500 (DbUpdateException) instead of 200/400")]
    [Priority(8)]
    public async Task UpdateProject_DescriptionOf1000VisibleCharactersInAParagraph_ShouldNotFail()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var project = await CreateProjectAsync(accessToken, resume.Id);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateProjectEndpoint, UpdateProjectRequest, ErrorResponse>(
                new UpdateProjectRequest(
                    project.Id, resume.Id, project.Tagline, project.Name, project.Url,
                    project.StartDate, project.EndDate, "<p>" + new string('a', 1000) + "</p>"));

        httpResponse.StatusCode.ShouldBeOneOf(HttpStatusCode.NoContent, HttpStatusCode.BadRequest);
    }

    [Fact(Skip = "#149: UpdateProjectValidator has no future StartDate rule (CreateProjectValidator has); update answers 204 instead of 400")]
    [Priority(9)]
    public async Task UpdateProject_FutureStartDate_ShouldReturnBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var project = await CreateProjectAsync(accessToken, resume.Id);

        var (httpResponse, error) = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateProjectEndpoint, UpdateProjectRequest, ErrorResponse>(
                new UpdateProjectRequest(
                    project.Id, resume.Id, project.Tagline, project.Name, project.Url,
                    new DateOnly(2099, 1, 1), null, project.Description));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        error.Errors.Keys.ShouldBe(["startDate"]);
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
