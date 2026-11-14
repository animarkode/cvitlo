using System.Net;
using Cvitlo.Resumes.Application.Features.Courses.CreateCourse;
using Cvitlo.Resumes.Application.Features.Courses.GetCourse;
using Cvitlo.Resumes.Application.Features.Courses.UpdateCourse;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Courses.UpdateCourse.Data;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Courses.UpdateCourse;

[Collection<CollectionResumes>]
[Priority(62)]
public class UpdateCourseTests(WebApp app) : TestBase
{
    [Fact]
    [Priority(0)]
    public async Task UpdateCourse_NoToken_ShouldReturnUnauthorized()
    {
        app.Client.DefaultRequestHeaders.Authorization = null;

        var httpResponse = await app.Client
            .PUTAsync<UpdateCourseEndpoint, UpdateCourseRequest>(
                new UpdateCourseRequest(Guid.NewGuid(), Guid.NewGuid(), "School", "Course", null, null, null));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Priority(1)]
    public async Task UpdateCourse_ShouldModifyFields()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = new CreateCourseRequestFaker(resume.Id).Generate();

        var (_, courseResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateCourseEndpoint, CreateCourseRequest, CreateCourseResponse>(createRequest);

        var updated = new CreateCourseRequestFaker(resume.Id, 62001).Generate();

        var updateRequest = new UpdateCourseRequest(
            courseResponse.Id,
            resume.Id,
            updated.School,
            updated.Name,
            updated.StartDate,
            updated.EndDate,
            updated.Description);

        var updateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateCourseEndpoint, UpdateCourseRequest>(updateRequest);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (_, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetCourseEndpoint, GetCourseRequest, GetCourseResponse>(
                new GetCourseRequest(resume.Id, courseResponse.Id));

        getResponse.School.ShouldBe(updated.School);
        getResponse.Name.ShouldBe(updated.Name);
    }

    [Fact]
    [Priority(2)]
    public async Task UpdateCourse_UnknownResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var course = new CreateCourseRequestFaker(Guid.NewGuid()).Generate();

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateCourseEndpoint, UpdateCourseRequest>(
                new UpdateCourseRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    course.School,
                    course.Name,
                    course.StartDate,
                    course.EndDate,
                    course.Description));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Priority(3)]
    public async Task CrossUserUpdateCourse_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);

        var createRequest = new CreateCourseRequestFaker(resume.Id).Generate();

        var (_, courseResponse) = await app.Client
            .WithAuthToken(userAToken)
            .POSTAsync<CreateCourseEndpoint, CreateCourseRequest, CreateCourseResponse>(createRequest);

        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);

        var updated = new CreateCourseRequestFaker(resume.Id, 62002).Generate();

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .PUTAsync<UpdateCourseEndpoint, UpdateCourseRequest>(
                new UpdateCourseRequest(
                    courseResponse.Id,
                    resume.Id,
                    updated.School,
                    updated.Name,
                    updated.StartDate,
                    updated.EndDate,
                    updated.Description));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [ClassData(typeof(InvalidUpdateCourseData))]
    [Priority(4)]
    public async Task UpdateCourse_InvalidField_ShouldReturnBadRequest(
        UpdateCourseRequest invalidRequest,
        string expectedKey)
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var created = await CreateCourseAsync(accessToken, resume.Id);

        var updateRequest = invalidRequest with { Id = created.Id, ResumeId = resume.Id };

        var (httpResponse, error) = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateCourseEndpoint, UpdateCourseRequest, ErrorResponse>(updateRequest);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        error.Errors.Keys.ShouldBe([expectedKey]);
    }

    [Fact]
    [Priority(5)]
    public async Task UpdateCourse_WithUnsafeDescription_ShouldPersistSanitizedHtml()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = new CreateCourseRequestFaker(resume.Id).Generate();

        var (_, courseResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateCourseEndpoint, CreateCourseRequest, CreateCourseResponse>(createRequest);

        const string unsafeDescription =
            "<p><strong>Module</strong> on XSS</p>"
            + "<script>alert('xss')</script>"
            + "<img src=x onerror=\"alert(1)\" />";

        var updateRequest = new UpdateCourseRequest(
            courseResponse.Id, resume.Id,
            "Udemy", "Security 101", null, null, unsafeDescription);

        var updateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateCourseEndpoint, UpdateCourseRequest>(updateRequest);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (_, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetCourseEndpoint, GetCourseRequest, GetCourseResponse>(
                new GetCourseRequest(resume.Id, courseResponse.Id));

        getResponse.Description.ShouldNotBeNull();
        getResponse.Description.ShouldNotContain("<script");
        getResponse.Description.ShouldNotContain("onerror");
        getResponse.Description.ShouldContain("<strong>Module</strong>");
    }

    [Fact]
    [Priority(6)]
    public async Task UpdateCourse_OwnResumeWithAnotherUsersItem_ShouldReturnNotFoundAndChangeNothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);
        var resumeA = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);
        var resumeB = await ResumesTestHelper.CreateResumeAsync(app.Client, userBToken, ct: ct);
        var item = await CreateCourseAsync(userAToken, resumeA.Id);

        var source = new CreateCourseRequestFaker(resumeB.Id, 62003).Generate() with { Name = "intruder" };

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .PUTAsync<UpdateCourseEndpoint, UpdateCourseRequest>(new UpdateCourseRequest(item.Id, resumeB.Id, source.School, source.Name, source.StartDate, source.EndDate, source.Description));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var (_, after) = await app.Client
            .WithAuthToken(userAToken)
            .GETAsync<GetCourseEndpoint, GetCourseRequest, GetCourseResponse>(
                new GetCourseRequest(resumeA.Id, item.Id));

        after.Name.ShouldBe(item.Name);
    }

    [Fact]
    [Priority(7)]
    public async Task UpdateCourse_UnknownItem_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var source = new CreateCourseRequestFaker(resume.Id).Generate();

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateCourseEndpoint, UpdateCourseRequest>(new UpdateCourseRequest(Guid.NewGuid(), resume.Id, source.School, source.Name, source.StartDate, source.EndDate, source.Description));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<CreateCourseResponse> CreateCourseAsync(
        string accessToken,
        Guid resumeId,
        int seed = ResumesTestConstants.CourseSeed)
    {
        var (_, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateCourseEndpoint, CreateCourseRequest, CreateCourseResponse>(
                new CreateCourseRequestFaker(resumeId, seed).Generate());

        return response;
    }
}
