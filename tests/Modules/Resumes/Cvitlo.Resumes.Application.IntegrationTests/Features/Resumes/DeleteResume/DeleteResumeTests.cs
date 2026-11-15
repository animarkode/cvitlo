using System.Net;
using Cvitlo.Resumes.Application.Features.Resumes.CreateResume;
using Cvitlo.Resumes.Application.Features.Resumes.DeleteResume;
using Cvitlo.Resumes.Application.Features.Resumes.GetResume;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Infrastructure.Database;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Resumes.DeleteResume;

[Collection<CollectionResumes>]
public class DeleteResumeTests(WebApp app) : TestBase
{
    [Fact]
    public async Task DeleteResume_NoToken_ShouldReturnUnauthorized()
    {
        var httpResponse = await app.Client
            .DELETEAsync<DeleteResumeEndpoint, DeleteResumeRequest>(
                new DeleteResumeRequest(Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteResume_ExistingResume_ShouldRemoveResume()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var createRequest = new CreateResumeRequest("en-us");

        var (_, createResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateResumeEndpoint, CreateResumeRequest, CreateResumeResponse>(createRequest);

        var deleteRequest = new DeleteResumeRequest(createResponse.Id);

        var deleteHttpResponse = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteResumeEndpoint, DeleteResumeRequest>(deleteRequest);

        deleteHttpResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getRequest = new GetResumeRequest(createResponse.Id);

        var (getHttpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(getRequest);

        getHttpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteResume_UnknownResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteResumeEndpoint, DeleteResumeRequest>(
                new DeleteResumeRequest(Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteResume_OtherUsersResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);

        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .DELETEAsync<DeleteResumeEndpoint, DeleteResumeRequest>(
                new DeleteResumeRequest(resume.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteResume_ResumeWithEverySection_ShouldRemoveEveryChildRow()
    {
        var ct = TestContext.Current.CancellationToken;
        var token = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var seeded = await ResumeWithEverySectionSeeder.SeedAsync(app, token, ct);

        var deleted = await app.Client
            .WithAuthToken(token)
            .DELETEAsync<DeleteResumeEndpoint, DeleteResumeRequest>(new DeleteResumeRequest(seeded.ResumeId));
        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ResumesDbContext>();
        var id = seeded.ResumeId;
        (await db.Resumes.CountAsync(x => x.Id == id, ct)).ShouldBe(0);
        (await db.Persons.CountAsync(x => x.ResumeId == id, ct)).ShouldBe(0);
        (await db.Experiences.CountAsync(x => x.ResumeId == id, ct)).ShouldBe(0);
        (await db.Educations.CountAsync(x => x.ResumeId == id, ct)).ShouldBe(0);
        (await db.Skills.CountAsync(x => x.ResumeId == id, ct)).ShouldBe(0);
        (await db.Languages.CountAsync(x => x.ResumeId == id, ct)).ShouldBe(0);
        (await db.Courses.CountAsync(x => x.ResumeId == id, ct)).ShouldBe(0);
        (await db.Projects.CountAsync(x => x.ResumeId == id, ct)).ShouldBe(0);
        (await db.Links.CountAsync(x => x.ResumeId == id, ct)).ShouldBe(0);
    }

    [Fact]
    public async Task DeleteResume_AlreadyDeleted_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var token = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, token, ct: ct);
        var request = new DeleteResumeRequest(resume.Id);

        var first = await app.Client
            .WithAuthToken(token)
            .DELETEAsync<DeleteResumeEndpoint, DeleteResumeRequest>(request);
        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var second = await app.Client
            .WithAuthToken(token)
            .DELETEAsync<DeleteResumeEndpoint, DeleteResumeRequest>(request);

        second.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
