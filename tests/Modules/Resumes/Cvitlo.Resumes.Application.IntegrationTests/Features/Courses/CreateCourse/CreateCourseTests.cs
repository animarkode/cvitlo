using System.Net;
using Cvitlo.Resumes.Application.Features.Courses.CreateCourse;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Courses.CreateCourse.Data;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;
using Cvitlo.Resumes.Application.Features.Courses.GetCourse;
using Cvitlo.Resumes.Application.Features.Courses.DeleteCourse;
using Cvitlo.Resumes.Application.Features.Courses.GetAllCourses;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Courses.CreateCourse;

[Collection<CollectionResumes>]
public class CreateCourseTests(WebApp app) : TestBase
{
    [Theory]
    [ClassData(typeof(ValidCourseData))]
    public async Task CreateCourse_ValidRequest_ShouldReturnTheCreatedItem(CreateCourseRequest request)
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = request with { ResumeId = resume.Id };

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateCourseEndpoint, CreateCourseRequest, CreateCourseResponse>(createRequest);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Id.ShouldNotBe(Guid.Empty);
        response.ResumeId.ShouldBe(resume.Id);
        response.School.ShouldBe(request.School);
        response.Name.ShouldBe(request.Name);
        response.StartDate.ShouldBe(request.StartDate);
        response.EndDate.ShouldBe(request.EndDate);
        response.Description.ShouldBe(request.Description);
        response.Order.ShouldBe(0);
    }

    [Fact]
    public async Task CreateCourse_UnknownResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var request = new CreateCourseRequestFaker(Guid.NewGuid()).Generate();

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateCourseEndpoint, CreateCourseRequest, CreateCourseResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [ClassData(typeof(InvalidCourseData))]
    public async Task CreateCourse_InvalidField_ShouldReturnBadRequest(
        CreateCourseRequest invalidRequest,
        string expectedKey)
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = invalidRequest with { ResumeId = resume.Id };

        var (httpResponse, error) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateCourseEndpoint, CreateCourseRequest, ErrorResponse>(createRequest);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        error.Errors.Keys.ShouldBe([expectedKey]);
    }

    [Fact]
    public async Task CreateCourse_FieldsAtMaximumLength_ShouldReturnOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var request = new CreateCourseRequestFaker(resume.Id).Generate() with { Name = new string('a', 100), School = new string('a', 100), Description = new string('a', 1000) };

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateCourseEndpoint, CreateCourseRequest, CreateCourseResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Name!.Length.ShouldBe(100);
        response.School!.Length.ShouldBe(100);
        response.Description!.Length.ShouldBe(1000);
    }

    [Fact]
    public async Task CreateCourse_StartDateToday_ShouldReturnOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var request = new CreateCourseRequestFaker(resume.Id).Generate() with { StartDate = today, EndDate = null };

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateCourseEndpoint, CreateCourseRequest, CreateCourseResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.StartDate.ShouldBe(today);
    }

    [Fact]
    public async Task CreateCourse_UnsafeDescription_ShouldStoreSanitizedHtml()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        const string unsafeDescription =
            "<p><strong>Module</strong> on XSS</p>"
            + "<script>alert('xss')</script>"
            + "<img src=x onerror=\"alert(1)\" />";

        var request = new CreateCourseRequestFaker(resume.Id).Generate() with { Description = unsafeDescription };

        var (createResponse, created) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateCourseEndpoint, CreateCourseRequest, CreateCourseResponse>(request);

        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var (_, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetCourseEndpoint, GetCourseRequest, GetCourseResponse>(
                new GetCourseRequest(resume.Id, created.Id));

        getResponse.Description.ShouldNotBeNull();
        getResponse.Description.ShouldNotContain("<script");
        getResponse.Description.ShouldNotContain("onerror");
        getResponse.Description.ShouldContain("<strong>Module</strong>");
    }

    [Fact]
    public async Task CreateCourse_DescriptionThatGrowsWhenEncoded_ShouldReturnBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var request = new CreateCourseRequestFaker(resume.Id).Generate() with { Description = new string('&', 1000) };

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateCourseEndpoint, CreateCourseRequest, ErrorResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCourse_DescriptionOfExactly1000CharactersThatDoesNotGrow_ShouldReturnOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var request = new CreateCourseRequestFaker(resume.Id).Generate() with { Description = new string('a', 1000) };

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateCourseEndpoint, CreateCourseRequest, CreateCourseResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateCourse_SeveralItems_ShouldNumberThemInCreationOrder()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var first = await CreateCourseAsync(accessToken, resume.Id, 65001);
        var second = await CreateCourseAsync(accessToken, resume.Id, 65002);
        var third = await CreateCourseAsync(accessToken, resume.Id, 65003);

        new[] { first.Order, second.Order, third.Order }.ShouldBe(new[] { 0, 1, 2 });

        var (_, all) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetAllCoursesEndpoint, GetAllCoursesRequest, GetAllCoursesResponse>(
                new GetAllCoursesRequest(resume.Id));

        all.Items.Select(i => i.Id).ShouldBe([first.Id, second.Id, third.Id]);
        all.Items.Select(i => i.Order).ShouldBe(new[] { 0, 1, 2 });
    }

    [Fact]
    public async Task CreateCourse_AfterDeletingTheMiddleItem_ShouldAppendAfterTheLast()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var first = await CreateCourseAsync(accessToken, resume.Id, 65001);
        var middle = await CreateCourseAsync(accessToken, resume.Id, 65002);
        var last = await CreateCourseAsync(accessToken, resume.Id, 65003);

        var deleteResponse = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteCourseEndpoint, DeleteCourseRequest>(new DeleteCourseRequest(resume.Id, middle.Id));

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var appended = await CreateCourseAsync(accessToken, resume.Id, 65004);

        appended.Order.ShouldBe(3);

        var (_, all) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetAllCoursesEndpoint, GetAllCoursesRequest, GetAllCoursesResponse>(
                new GetAllCoursesRequest(resume.Id));

        all.Items.Select(i => i.Id).ShouldBe([first.Id, last.Id, appended.Id]);
        all.Items.Select(i => i.Order).ShouldBe(new[] { 0, 2, 3 });
    }

    [Fact]
    public async Task CreateCourse_NoToken_ShouldReturnUnauthorized()
    {
        var (httpResponse, _) = await app.Client
            .POSTAsync<CreateCourseEndpoint, CreateCourseRequest, CreateCourseResponse>(
                new CreateCourseRequestFaker(Guid.NewGuid()).Generate());

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
