using System.Net;
using Cvitlo.Resumes.Application.Features.Courses.CreateCourse;
using Cvitlo.Resumes.Application.Features.Courses.GetAllCourses;
using Cvitlo.Resumes.Application.Features.Courses.GetCourse;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Courses.GetCourse;

[Collection<CollectionResumes>]
public class GetCourseTests(WebApp app) : TestBase
{
    [Fact]
    public async Task GetCourse_NoToken_ShouldReturnUnauthorized()
    {
        var (httpResponse, _) = await app.Client
            .GETAsync<GetCourseEndpoint, GetCourseRequest, GetCourseResponse>(
                new GetCourseRequest(Guid.NewGuid(), Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCourse_ExistingItem_ShouldReturnTheCreatedCourse()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = new CreateCourseRequestFaker(resume.Id).Generate();

        var (_, courseResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateCourseEndpoint, CreateCourseRequest, CreateCourseResponse>(createRequest);

        var (httpResponse, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetCourseEndpoint, GetCourseRequest, GetCourseResponse>(
                new GetCourseRequest(resume.Id, courseResponse.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        getResponse.Id.ShouldBe(courseResponse.Id);
        getResponse.School.ShouldBe(createRequest.School);
        getResponse.Name.ShouldBe(createRequest.Name);
    }

    [Fact]
    public async Task GetAllCourses_ResumeWithTwoItems_ShouldReturnBoth()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var items = new CreateCourseRequestFaker(resume.Id).Generate(2);

        await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateCourseEndpoint, CreateCourseRequest, CreateCourseResponse>(items[0]);

        await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateCourseEndpoint, CreateCourseRequest, CreateCourseResponse>(items[1]);

        var (httpResponse, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetAllCoursesEndpoint, GetAllCoursesRequest, GetAllCoursesResponse>(
                new GetAllCoursesRequest(resume.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        getResponse.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetCourse_UnknownCourse_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetCourseEndpoint, GetCourseRequest, GetCourseResponse>(
                new GetCourseRequest(resume.Id, Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllCourses_UnknownResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetAllCoursesEndpoint, GetAllCoursesRequest, GetAllCoursesResponse>(
                new GetAllCoursesRequest(Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllCourses_ResumeWithoutCourses_ShouldReturnEmptyList()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var (httpResponse, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetAllCoursesEndpoint, GetAllCoursesRequest, GetAllCoursesResponse>(
                new GetAllCoursesRequest(resume.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        getResponse.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetCourse_OtherUsersResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);

        var createRequest = new CreateCourseRequestFaker(resume.Id).Generate();

        var (_, courseResponse) = await app.Client
            .WithAuthToken(userAToken)
            .POSTAsync<CreateCourseEndpoint, CreateCourseRequest, CreateCourseResponse>(createRequest);

        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(userBToken)
            .GETAsync<GetCourseEndpoint, GetCourseRequest, GetCourseResponse>(
                new GetCourseRequest(resume.Id, courseResponse.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllCourses_OtherUsersResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);

        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(userBToken)
            .GETAsync<GetAllCoursesEndpoint, GetAllCoursesRequest, GetAllCoursesResponse>(
                new GetAllCoursesRequest(resume.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetCourse_OwnResumeWithAnotherUsersItem_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);
        var resumeA = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);
        var resumeB = await ResumesTestHelper.CreateResumeAsync(app.Client, userBToken, ct: ct);
        var item = await CreateCourseAsync(userAToken, resumeA.Id);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(userBToken)
            .GETAsync<GetCourseEndpoint, GetCourseRequest, GetCourseResponse>(
                new GetCourseRequest(resumeB.Id, item.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCourse_CourseOfTheCallersOtherResume_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resumeA = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var resumeA2 = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var item = await CreateCourseAsync(accessToken, resumeA.Id);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetCourseEndpoint, GetCourseRequest, GetCourseResponse>(
                new GetCourseRequest(resumeA2.Id, item.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllCourses_NoToken_ShouldReturnUnauthorized()
    {
        var (httpResponse, _) = await app.Client
            .GETAsync<GetAllCoursesEndpoint, GetAllCoursesRequest, GetAllCoursesResponse>(
                new GetAllCoursesRequest(Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
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
