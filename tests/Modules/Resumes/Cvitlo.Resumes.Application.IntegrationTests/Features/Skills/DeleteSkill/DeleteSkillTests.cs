using System.Net;
using Cvitlo.Resumes.Application.Features.Skills.CreateSkill;
using Cvitlo.Resumes.Application.Features.Skills.DeleteSkill;
using Cvitlo.Resumes.Application.Features.Skills.GetSkill;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Skills.DeleteSkill;

[Collection<CollectionResumes>]
[Priority(43)]
public class DeleteSkillTests(WebApp app) : TestBase
{
    [Fact]
    [Priority(0)]
    public async Task DeleteSkill_NoToken_ShouldReturnUnauthorized()
    {
        var httpResponse = await app.Client
            .DELETEAsync<DeleteSkillEndpoint, DeleteSkillRequest>(
                new DeleteSkillRequest(Guid.NewGuid(), Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Priority(1)]
    public async Task DeleteSkill_ExistingItem_ShouldRemoveIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = new CreateSkillRequestFaker(resume.Id).Generate();

        var (_, skillResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateSkillEndpoint, CreateSkillRequest, CreateSkillResponse>(createRequest);

        var deleteResponse = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteSkillEndpoint, DeleteSkillRequest>(
                new DeleteSkillRequest(resume.Id, skillResponse.Id));

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (getResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetSkillEndpoint, GetSkillRequest, GetSkillResponse>(
                new GetSkillRequest(resume.Id, skillResponse.Id));

        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Priority(2)]
    public async Task DeleteSkill_UnknownSkill_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteSkillEndpoint, DeleteSkillRequest>(
                new DeleteSkillRequest(resume.Id, Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Priority(3)]
    public async Task DeleteSkill_OtherUsersResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);

        var createRequest = new CreateSkillRequestFaker(resume.Id).Generate();

        var (_, skillResponse) = await app.Client
            .WithAuthToken(userAToken)
            .POSTAsync<CreateSkillEndpoint, CreateSkillRequest, CreateSkillResponse>(createRequest);

        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .DELETEAsync<DeleteSkillEndpoint, DeleteSkillRequest>(
                new DeleteSkillRequest(resume.Id, skillResponse.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Priority(4)]
    public async Task DeleteSkill_OwnResumeWithAnotherUsersItem_ShouldReturnNotFoundAndKeepIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);
        var resumeA = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);
        var resumeB = await ResumesTestHelper.CreateResumeAsync(app.Client, userBToken, ct: ct);
        var item = await CreateSkillAsync(userAToken, resumeA.Id);

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .DELETEAsync<DeleteSkillEndpoint, DeleteSkillRequest>(
                new DeleteSkillRequest(resumeB.Id, item.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var (getResponse, _) = await app.Client
            .WithAuthToken(userAToken)
            .GETAsync<GetSkillEndpoint, GetSkillRequest, GetSkillResponse>(
                new GetSkillRequest(resumeA.Id, item.Id));

        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task<CreateSkillResponse> CreateSkillAsync(
        string accessToken,
        Guid resumeId,
        int seed = ResumesTestConstants.SkillSeed)
    {
        var (_, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateSkillEndpoint, CreateSkillRequest, CreateSkillResponse>(
                new CreateSkillRequestFaker(resumeId, seed).Generate());

        return response;
    }

    [Fact]
    [Priority(5)]
    public async Task DeleteSkill_AlreadyDeleted_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var item = await CreateSkillAsync(accessToken, resume.Id);

        var first = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteSkillEndpoint, DeleteSkillRequest>(new DeleteSkillRequest(resume.Id, item.Id));

        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var second = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteSkillEndpoint, DeleteSkillRequest>(new DeleteSkillRequest(resume.Id, item.Id));

        second.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
