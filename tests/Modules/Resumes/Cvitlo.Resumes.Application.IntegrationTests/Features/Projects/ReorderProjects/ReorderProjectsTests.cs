using System.Text;
using System.Net;
using Cvitlo.Resumes.Application.Features.Projects.CreateProject;
using Cvitlo.Resumes.Application.Features.Projects.ReorderProjects;
using Cvitlo.Resumes.Application.Features.Resumes.GetResume;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Projects.ReorderProjects;

[Collection<CollectionResumes>]
public class ReorderProjectsTests(WebApp app) : TestBase
{
    [Fact]
    public async Task ReorderProjects_NoToken_ShouldReturnUnauthorized()
    {
        var httpResponse = await app.Client
            .PUTAsync<ReorderProjectsEndpoint, ReorderProjectsRequest>(
                new ReorderProjectsRequest(Guid.NewGuid(), new List<Guid> { Guid.NewGuid() }));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReorderProjects_NewOrder_ShouldPersistIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var ids = await CreateProjectsAsync(accessToken, resume.Id, 3);

        var newOrder = new List<Guid> { ids[2], ids[0], ids[1] };

        var reorderResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<ReorderProjectsEndpoint, ReorderProjectsRequest>(
                new ReorderProjectsRequest(resume.Id, newOrder));

        reorderResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (_, resumeResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(
                new GetResumeRequest(resume.Id));

        resumeResponse.Projects.Select(c => c.Id).ShouldBe(newOrder);
        resumeResponse.Projects.Select(c => c.Order).ShouldBe(new[] { 0, 1, 2 });
    }

    [Fact]
    public async Task ReorderProjects_UnknownId_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var ids = await CreateProjectsAsync(accessToken, resume.Id, 2);

        var mismatched = new List<Guid> { ids[0], Guid.NewGuid() };

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<ReorderProjectsEndpoint, ReorderProjectsRequest>(
                new ReorderProjectsRequest(resume.Id, mismatched));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReorderProjects_EmptyOrderedIds_ShouldReturnBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<ReorderProjectsEndpoint, ReorderProjectsRequest>(
                new ReorderProjectsRequest(resume.Id, new List<Guid>()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReorderProjects_DuplicateOrderedIds_ShouldReturnBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var duplicate = Guid.NewGuid();

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<ReorderProjectsEndpoint, ReorderProjectsRequest>(
                new ReorderProjectsRequest(resume.Id, new List<Guid> { duplicate, duplicate }));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReorderProjects_OtherUsersResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);

        var ids = await CreateProjectsAsync(userAToken, resume.Id, 2);

        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .PUTAsync<ReorderProjectsEndpoint, ReorderProjectsRequest>(
                new ReorderProjectsRequest(resume.Id, new List<Guid> { ids[1], ids[0] }));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReorderProjects_IdsFromAnotherResume_ShouldReturnNotFoundAndKeepOrder()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);
        var resumeA = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);
        var resumeB = await ResumesTestHelper.CreateResumeAsync(app.Client, userBToken, ct: ct);
        var foreignIds = await CreateProjectsAsync(userAToken, resumeA.Id, 1);
        var ownIds = await CreateProjectsAsync(userBToken, resumeB.Id, 2);

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .PUTAsync<ReorderProjectsEndpoint, ReorderProjectsRequest>(
                new ReorderProjectsRequest(resumeB.Id, new List<Guid> { foreignIds[0], ownIds[0] }));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var (_, resumeResponse) = await app.Client
            .WithAuthToken(userBToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(
                new GetResumeRequest(resumeB.Id));

        resumeResponse.Projects.Select(i => i.Id).ShouldBe(ownIds);
    }

    [Fact]
    public async Task ReorderProjects_FewerIdsThanItems_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var ids = await CreateProjectsAsync(accessToken, resume.Id, 3);

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<ReorderProjectsEndpoint, ReorderProjectsRequest>(
                new ReorderProjectsRequest(resume.Id, new List<Guid> { ids[0], ids[1] }));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(Skip = "#146: When() on the OrderedIds chain skips NotEmpty, SectionReorder dereferences null; body {} answers 500 (NullReferenceException) instead of 400")]
    public async Task ReorderProjects_BodyWithoutOrderedIds_ShouldReturnBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        using var body = new StringContent("{}", Encoding.UTF8, "application/json");
        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PutAsync($"api/v1/resumes/{resume.Id}/projects/reorder", body, ct);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task<List<Guid>> CreateProjectsAsync(string accessToken, Guid resumeId, int count)
    {
        var ids = new List<Guid>();

        for (var i = 0; i < count; i++)
        {
            var createRequest = new CreateProjectRequestFaker(resumeId, 64010 + i).Generate();

            var (_, response) = await app.Client
                .WithAuthToken(accessToken)
                .POSTAsync<CreateProjectEndpoint, CreateProjectRequest, CreateProjectResponse>(createRequest);

            ids.Add(response.Id);
        }

        return ids;
    }
}
