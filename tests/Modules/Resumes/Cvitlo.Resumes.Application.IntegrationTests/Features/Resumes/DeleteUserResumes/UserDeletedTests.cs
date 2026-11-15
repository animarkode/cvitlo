using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Cvitlo.Auth.PublicApi.Events;
using Cvitlo.Common.Testing.Data;
using Cvitlo.Resumes.Application.Features.Experiences.CreateExperience;
using Cvitlo.Resumes.Application.Features.Resumes.GetAllResumes;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Infrastructure.Database;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Resumes.DeleteUserResumes;

[Collection<CollectionResumes>]
public class UserDeletedTests(WebApp app) : TestBase
{
    [Fact]
    public async Task UserDeleted_UserWithResumes_ShouldRemoveOnlyThatUsersData()
    {
        var ct = TestContext.Current.CancellationToken;
        var (tokenA, subA) = await SignInNewUserAsync(9501, ct);
        var (tokenB, _) = await SignInNewUserAsync(9502, ct);
        var resumeA1 = await SeedResumeWithExperienceAsync(tokenA, ct);
        var resumeA2 = await SeedResumeWithExperienceAsync(tokenA, ct);
        var resumeB = await SeedResumeWithExperienceAsync(tokenB, ct);

        await new UserDeleted(subA).PublishAsync(Mode.WaitForAll, ct);

        var resumeIds = new[] { resumeA1, resumeA2, resumeB };
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ResumesDbContext>();
        var remainingResumes = await db.Resumes.Where(r => resumeIds.Contains(r.Id)).Select(r => r.Id).ToListAsync(ct);
        var remainingExperiences = await db.Experiences.Where(e => resumeIds.Contains(e.ResumeId))
            .Select(e => e.ResumeId).ToListAsync(ct);

        remainingResumes.ShouldBe([resumeB]);
        remainingExperiences.ShouldBe([resumeB]);
    }

    [Fact]
    public async Task UserDeleted_PublishedTwice_ShouldNotThrowAndKeepSameState()
    {
        var ct = TestContext.Current.CancellationToken;
        var (tokenA, subA) = await SignInNewUserAsync(9511, ct);
        var (tokenB, _) = await SignInNewUserAsync(9512, ct);
        await SeedResumeWithExperienceAsync(tokenA, ct);
        var resumeB = await SeedResumeWithExperienceAsync(tokenB, ct);

        await new UserDeleted(subA).PublishAsync(Mode.WaitForAll, ct);
        await Should.NotThrowAsync(() => new UserDeleted(subA).PublishAsync(Mode.WaitForAll, ct));

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ResumesDbContext>();
        (await db.Resumes.AnyAsync(r => r.UserId == Guid.Parse(subA), ct)).ShouldBeFalse();
        (await db.Resumes.AnyAsync(r => r.Id == resumeB, ct)).ShouldBeTrue();
    }

    [Fact]
    public async Task UserDeleted_AfterCachedList_ShouldEmptyTheList()
    {
        var ct = TestContext.Current.CancellationToken;
        var (tokenA, subA) = await SignInNewUserAsync(9521, ct);
        await SeedResumeWithExperienceAsync(tokenA, ct);

        var (warmResponse, warm) = await app.Client
            .WithAuthToken(tokenA)
            .GETAsync<GetAllResumesEndpoint, GetAllResumesResponse>();
        warmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        warm.Items.ShouldNotBeEmpty();

        await new UserDeleted(subA).PublishAsync(Mode.WaitForAll, ct);

        var (httpResponse, response) = await app.Client
            .WithAuthToken(tokenA)
            .GETAsync<GetAllResumesEndpoint, GetAllResumesResponse>();
        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task UserDeleted_NonGuidSubject_ShouldBeNoOp()
    {
        var ct = TestContext.Current.CancellationToken;
        var (tokenA, _) = await SignInNewUserAsync(9531, ct);
        var resumeId = await SeedResumeWithExperienceAsync(tokenA, ct);

        await Should.NotThrowAsync(() => new UserDeleted("not-a-guid").PublishAsync(Mode.WaitForAll, ct));

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ResumesDbContext>();
        (await db.Resumes.AnyAsync(r => r.Id == resumeId, ct)).ShouldBeTrue();
    }

    // A fresh user per test: deleting its data cannot affect any other test.
    private async Task<(string Token, string Subject)> SignInNewUserAsync(int seed, CancellationToken ct)
    {
        var user = new TestUserFaker(seed).Generate();
        await app.Keycloak.Admin.CreateUserAsync(user, ct: ct);
        var token = await app.GetAccessTokenAsync(user, ct);
        var subject = new JwtSecurityTokenHandler().ReadJwtToken(token).Subject;

        return (token, subject);
    }

    private async Task<Guid> SeedResumeWithExperienceAsync(string token, CancellationToken ct)
    {
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, token, ct: ct);
        var request = new CreateExperienceRequestFaker(resume.Id).Generate();

        var (httpResponse, _) = await app.Client
            .WithAuthToken(token)
            .POSTAsync<CreateExperienceEndpoint, CreateExperienceRequest, CreateExperienceResponse>(request);
        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        return resume.Id;
    }
}
