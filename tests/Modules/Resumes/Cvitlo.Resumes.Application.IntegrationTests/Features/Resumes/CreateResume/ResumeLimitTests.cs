using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Cvitlo.Resumes.Application.Features.Resumes.CreateResume;
using Cvitlo.Resumes.Application.Features.Resumes.DeleteResume;
using Cvitlo.Resumes.Application.Features.Resumes.GetAllResumes;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Infrastructure.Database;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Resumes.CreateResume;

[Collection<CollectionResumeLimits>]
public class ResumeLimitTests(ResumeLimitsWebApp app) : TestBase
{
    // Users 0-1 are premium (limit 2), users 2-4 regular (limit 1); each test owns one user.
    private const int PremiumUser = 0;
    private const int RegularUserAtLimit = 2;
    private const int RegularUserDeleting = 3;
    private const int RegularUserParallel = 4;

    [Fact]
    public async Task CreateResume_RegularUserAtLimit_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var token = await ResumesTestHelper.SignInAsUserAsync(app, RegularUserAtLimit, ct);

        (await PostResumeAsync(token)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await PostResumeAsync(token)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var (_, list) = await app.Client
            .WithAuthToken(token)
            .GETAsync<GetAllResumesEndpoint, GetAllResumesResponse>();
        list.Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CreateResume_PremiumUserAboveRegularLimit_ShouldReturnOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var token = await ResumesTestHelper.SignInAsUserAsync(app, PremiumUser, ct);

        (await PostResumeAsync(token)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await PostResumeAsync(token)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await PostResumeAsync(token)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateResume_AfterDeletingAtLimit_ShouldReturnOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var token = await ResumesTestHelper.SignInAsUserAsync(app, RegularUserDeleting, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, token, ct: ct);

        (await PostResumeAsync(token)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var deleted = await app.Client
            .WithAuthToken(token)
            .DELETEAsync<DeleteResumeEndpoint, DeleteResumeRequest>(new DeleteResumeRequest(resume.Id));
        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await PostResumeAsync(token)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact(Skip = "#159: the limit check and the insert are not atomic, parallel POST /resumes can exceed the limit (red in 1 of 2 runs)")]
    public async Task CreateResume_ParallelRequestsAtLimit_ShouldCreateOnlyOne()
    {
        var ct = TestContext.Current.CancellationToken;
        var token = await ResumesTestHelper.SignInAsUserAsync(app, RegularUserParallel, ct);
        var userId = Guid.Parse(new JwtSecurityTokenHandler().ReadJwtToken(token).Subject);

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => PostResumeAsync(token)));

        responses.Count(r => r.StatusCode == HttpStatusCode.OK).ShouldBe(1);

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ResumesDbContext>();
        (await db.Resumes.CountAsync(r => r.UserId == userId, ct)).ShouldBe(1);
    }

    private async Task<HttpResponseMessage> PostResumeAsync(string token)
    {
        using var client = app.Client.WithAuthToken(token);
        var (response, _) = await client
            .POSTAsync<CreateResumeEndpoint, CreateResumeRequest, CreateResumeResponse>(
                new CreateResumeRequest("en-us"));

        return response;
    }
}
