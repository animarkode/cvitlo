using System.Net;
using System.Net.Http.Json;
using Cvitlo.Resumes.Application.Features.Experiences.CreateExperience;
using Cvitlo.Resumes.Application.Features.Experiences.ReorderExperiences;
using Cvitlo.Resumes.Application.Features.Resumes.GetResume;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;
using System.Text;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Experiences.ReorderExperiences;

[Collection<CollectionResumes>]
public class ReorderExperiencesTests(WebApp app) : TestBase
{
    [Fact]
    public async Task ReorderExperiences_NoToken_ShouldReturnUnauthorized()
    {
        var httpResponse = await app.Client
            .PUTAsync<ReorderExperiencesEndpoint, ReorderExperiencesRequest>(
                new ReorderExperiencesRequest(Guid.NewGuid(), new List<Guid> { Guid.NewGuid() }));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReorderExperiences_NewOrder_ShouldPersistIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var ids = await CreateExperiencesAsync(accessToken, resume.Id, 3);

        var newOrder = new List<Guid> { ids[2], ids[0], ids[1] };

        var reorderResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<ReorderExperiencesEndpoint, ReorderExperiencesRequest>(
                new ReorderExperiencesRequest(resume.Id, newOrder));

        reorderResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (_, resumeResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(
                new GetResumeRequest(resume.Id));

        resumeResponse.Experience.Select(e => e.Id).ShouldBe(newOrder);
        resumeResponse.Experience.Select(e => e.Order).ShouldBe(new[] { 0, 1, 2 });
    }

    [Fact]
    public async Task ReorderExperiences_UnknownId_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var ids = await CreateExperiencesAsync(accessToken, resume.Id, 2);

        var mismatched = new List<Guid> { ids[0], Guid.NewGuid() };

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<ReorderExperiencesEndpoint, ReorderExperiencesRequest>(
                new ReorderExperiencesRequest(resume.Id, mismatched));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReorderExperiences_EmptyOrderedIds_ShouldReturnBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<ReorderExperiencesEndpoint, ReorderExperiencesRequest>(
                new ReorderExperiencesRequest(resume.Id, new List<Guid>()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReorderExperiences_DuplicateOrderedIds_ShouldReturnBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var duplicate = Guid.NewGuid();

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<ReorderExperiencesEndpoint, ReorderExperiencesRequest>(
                new ReorderExperiencesRequest(resume.Id, new List<Guid> { duplicate, duplicate }));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReorderExperiences_OtherUsersResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);

        var ids = await CreateExperiencesAsync(userAToken, resume.Id, 2);

        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .PUTAsync<ReorderExperiencesEndpoint, ReorderExperiencesRequest>(
                new ReorderExperiencesRequest(resume.Id, new List<Guid> { ids[1], ids[0] }));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReorderExperiences_IdsFromAnotherResume_ShouldReturnNotFoundAndKeepOrder()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);
        var resumeA = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);
        var resumeB = await ResumesTestHelper.CreateResumeAsync(app.Client, userBToken, ct: ct);
        var foreignIds = await CreateExperiencesAsync(userAToken, resumeA.Id, 1);
        var ownIds = await CreateExperiencesAsync(userBToken, resumeB.Id, 2);

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .PUTAsync<ReorderExperiencesEndpoint, ReorderExperiencesRequest>(
                new ReorderExperiencesRequest(resumeB.Id, new List<Guid> { foreignIds[0], ownIds[0] }));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var (_, resumeResponse) = await app.Client
            .WithAuthToken(userBToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(
                new GetResumeRequest(resumeB.Id));

        resumeResponse.Experience.Select(i => i.Id).ShouldBe(ownIds);
    }

    [Fact]
    public async Task ReorderExperiences_FewerIdsThanItems_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var ids = await CreateExperiencesAsync(accessToken, resume.Id, 3);

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<ReorderExperiencesEndpoint, ReorderExperiencesRequest>(
                new ReorderExperiencesRequest(resume.Id, new List<Guid> { ids[0], ids[1] }));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReorderExperiences_SameOrder_ShouldReturnNoContentAndKeepOrder()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var ids = await CreateExperiencesAsync(accessToken, resume.Id, 3);

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<ReorderExperiencesEndpoint, ReorderExperiencesRequest>(new ReorderExperiencesRequest(resume.Id, ids));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (_, resumeResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(
                new GetResumeRequest(resume.Id));

        resumeResponse.Experience.Select(i => i.Id).ShouldBe(ids);
        resumeResponse.Experience.Select(i => i.Order).ShouldBe(new[] { 0, 1, 2 });
    }

    [Fact]
    public async Task ReorderExperiences_BodyWithoutOrderedIds_ShouldReturnBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        using var body = new StringContent("{}", Encoding.UTF8, "application/json");
        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PutAsync($"api/v1/resumes/{resume.Id}/experiences/reorder", body, ct);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var error = await httpResponse.Content.ReadFromJsonAsync<ErrorResponse>(ct);
        error.ShouldNotBeNull();
        error.Errors.ShouldContainKey("orderedIds");
    }

    private async Task<List<Guid>> CreateExperiencesAsync(string accessToken, Guid resumeId, int count)
    {
        var ids = new List<Guid>();

        for (var i = 0; i < count; i++)
        {
            var createRequest = new CreateExperienceRequestFaker(resumeId, 34010 + i).Generate();

            var (_, response) = await app.Client
                .WithAuthToken(accessToken)
                .POSTAsync<CreateExperienceEndpoint, CreateExperienceRequest, CreateExperienceResponse>(
                    createRequest);

            ids.Add(response.Id);
        }

        return ids;
    }
}
