using System.Net;
using Cvitlo.Resumes.Application.Features.Courses.CreateCourse;
using Cvitlo.Resumes.Application.Features.Courses.DeleteCourse;
using Cvitlo.Resumes.Application.Features.Courses.GetCourse;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Courses.DeleteCourse;

[Collection<CollectionResumes>]
[Priority(63)]
public class DeleteCourseTests(WebApp app) : TestBase
{
    [Fact]
    [Priority(0)]
    public async Task DeleteCourse_NoToken_ShouldReturnUnauthorized()
    {
        var httpResponse = await app.Client
            .DELETEAsync<DeleteCourseEndpoint, DeleteCourseRequest>(
                new DeleteCourseRequest(Guid.NewGuid(), Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Priority(1)]
    public async Task DeleteCourse_ExistingItem_ShouldRemoveIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = new CreateCourseRequestFaker(resume.Id).Generate();

        var (_, courseResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateCourseEndpoint, CreateCourseRequest, CreateCourseResponse>(createRequest);

        var deleteResponse = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteCourseEndpoint, DeleteCourseRequest>(
                new DeleteCourseRequest(resume.Id, courseResponse.Id));

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (getResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetCourseEndpoint, GetCourseRequest, GetCourseResponse>(
                new GetCourseRequest(resume.Id, courseResponse.Id));

        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Priority(2)]
    public async Task DeleteCourse_UnknownCourse_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteCourseEndpoint, DeleteCourseRequest>(
                new DeleteCourseRequest(resume.Id, Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Priority(3)]
    public async Task DeleteCourse_OtherUsersResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);

        var createRequest = new CreateCourseRequestFaker(resume.Id).Generate();

        var (_, courseResponse) = await app.Client
            .WithAuthToken(userAToken)
            .POSTAsync<CreateCourseEndpoint, CreateCourseRequest, CreateCourseResponse>(createRequest);

        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .DELETEAsync<DeleteCourseEndpoint, DeleteCourseRequest>(
                new DeleteCourseRequest(resume.Id, courseResponse.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Priority(4)]
    public async Task DeleteCourse_OwnResumeWithAnotherUsersItem_ShouldReturnNotFoundAndKeepIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);
        var resumeA = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);
        var resumeB = await ResumesTestHelper.CreateResumeAsync(app.Client, userBToken, ct: ct);
        var item = await CreateCourseAsync(userAToken, resumeA.Id);

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .DELETEAsync<DeleteCourseEndpoint, DeleteCourseRequest>(
                new DeleteCourseRequest(resumeB.Id, item.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var (getResponse, _) = await app.Client
            .WithAuthToken(userAToken)
            .GETAsync<GetCourseEndpoint, GetCourseRequest, GetCourseResponse>(
                new GetCourseRequest(resumeA.Id, item.Id));

        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Priority(5)]
    public async Task DeleteCourse_AlreadyDeleted_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var item = await CreateCourseAsync(accessToken, resume.Id);

        var first = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteCourseEndpoint, DeleteCourseRequest>(new DeleteCourseRequest(resume.Id, item.Id));

        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var second = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteCourseEndpoint, DeleteCourseRequest>(new DeleteCourseRequest(resume.Id, item.Id));

        second.StatusCode.ShouldBe(HttpStatusCode.NotFound);
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
