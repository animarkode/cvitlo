using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Faber.Modules.Auth.Application.Sessions;
using Faber.Modules.Auth.Application.Tests.Support;
using Faber.Modules.Auth.Infrastructure.Database;
using Faber.Modules.Resumes.Domain.Entities;
using Faber.Modules.Resumes.Infrastructure.Database;
using Faber.Testing.Shared.Data;
using Faber.Testing.Shared.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Faber.Modules.Auth.Application.Tests.Features.Account.DeleteAccount;

[Collection<CollectionAuth>]
public class DeleteAccountTests(WebApp app)
{
    private const string Route = "api/v1/auth/account";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task DeleteAccount_AfterARecentSignIn_ShouldEraseTheUserEverywhereAndLeaveOthersAlone()
    {
        app.Clock.Realign();
        var (userA, subjectA) = await CreateUserAsync(9301);
        var (userB, subjectB) = await CreateUserAsync(9302);
        var resumeA1 = await SeedResumeAsync(subjectA);
        var resumeA2 = await SeedResumeAsync(subjectA);
        var resumeB = await SeedResumeAsync(subjectB);
        var (client, _) = await app.SignInAsync(userA, ct: Ct);
        using var _client = client;
        var (otherDevice, _) = await app.SignInAsync(userA, ct: Ct);
        using var _otherDevice = otherDevice;
        var (clientB, _) = await app.SignInAsync(userB, ct: Ct);
        using var _clientB = clientB;
        (await CountSessionsAsync(subjectA)).ShouldBe(2);

        using var response = await client.DeleteAsync(Route, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await app.Keycloak.Admin.FindUserIdAsync(userA.Email, Ct)).ShouldBeNull();
        (await CountResumesAsync(resumeA1, resumeA2)).ShouldBe(0);
        (await CountExperiencesAsync(resumeA1, resumeA2)).ShouldBe(0);
        (await CountPendingErasuresAsync(subjectA)).ShouldBe(0, "a finished erasure leaves no record");
        (await CountSessionsAsync(subjectA)).ShouldBe(0, "every session of the user goes, not only this browser's");
        (await CountResumesAsync(resumeB)).ShouldBe(1);
        (await CountExperiencesAsync(resumeB)).ShouldBe(1);
        (await CountSessionsAsync(subjectB)).ShouldBe(1);
        using var after = await client.GetAsync("api/v1/auth/user", Ct);
        after.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        using var afterOther = await otherDevice.GetAsync("api/v1/auth/user", Ct);
        afterOther.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAccount_AfterAnOldSignIn_ShouldRequireReauthenticationAndKeepEverything()
    {
        app.Clock.Realign();
        var (user, subject) = await CreateUserAsync(9303);
        var resume = await SeedResumeAsync(subject);
        var (client, _) = await app.SignInAsync(user, ct: Ct);
        using var _client = client;
        app.Clock.Advance(TimeSpan.FromSeconds(301));

        using var response = await client.DeleteAsync(Route, Ct);

        await ShouldRequireReauthenticationAsync(response);
        (await app.Keycloak.Admin.FindUserIdAsync(user.Email, Ct)).ShouldBe(subject);
        (await CountResumesAsync(resume)).ShouldBe(1);
        (await CountSessionsAsync(subject)).ShouldBe(1);
    }

    [Fact]
    public async Task DeleteAccount_JustInsideTheFiveMinuteLimit_ShouldSucceed()
    {
        app.Clock.Realign();
        var (user, _) = await CreateUserAsync(9304);
        var (client, _) = await app.SignInAsync(user, ct: Ct);
        using var _client = client;
        app.Clock.Advance(TimeSpan.FromSeconds(240));

        using var response = await client.DeleteAsync(Route, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteAccount_AfterReauthenticating_ShouldSucceed()
    {
        app.Clock.Realign();
        var (user, subject) = await CreateUserAsync(9305);
        var resume = await SeedResumeAsync(subject);
        using var client = app.CreateBrowserClient();
        using var driver = new KeycloakLoginDriver(client);
        using var signedIn = await driver.SignInAsync(user.Email, user.Password, ct: Ct);
        app.Clock.Advance(TimeSpan.FromSeconds(301));
        using var refused = await client.DeleteAsync(Route, Ct);
        await ShouldRequireReauthenticationAsync(refused);
        app.Clock.Realign();

        var page = await driver.StartAsync("api/v1/auth/account-action/reauthenticate", Ct);
        using var submitted = await driver.SubmitAsync(page, "kc-form-login", new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["username"] = user.Email,
            ["password"] = user.Password,
            ["credentialId"] = string.Empty
        }, Ct);
        using var callback = await driver.FinishAtApiAsync(submitted, Ct);

        callback.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        callback.Headers.Location!.OriginalString.ShouldBe("/account/personal-info?action=reauthenticate&status=success");
        (await CountSessionsAsync(subject)).ShouldBe(1, "re-authenticating replaces the session");
        using var response = await client.DeleteAsync(Route, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await app.Keycloak.Admin.FindUserIdAsync(user.Email, Ct)).ShouldBeNull();
        (await CountResumesAsync(resume)).ShouldBe(0);
    }

    [Fact]
    public async Task DeleteAccount_WithoutCsrfHeader_ShouldBeRefusedAndKeepTheUser()
    {
        app.Clock.Realign();
        var (user, _) = await CreateUserAsync(9306);
        var (client, _) = await app.SignInAsync(user, ct: Ct);
        using var _client = client;
        client.DefaultRequestHeaders.Remove("X-CSRF");

        using var response = await client.DeleteAsync(Route, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await app.Keycloak.Admin.FindUserIdAsync(user.Email, Ct)).ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteAccount_WithoutSession_ShouldReturnUnauthorized()
    {
        using var client = app.CreateBrowserClient();

        using var response = await client.DeleteAsync(Route, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAccount_WithABearerToken_ShouldBeRefusedAndKeepTheUser()
    {
        app.Clock.Realign();
        var (user, _) = await CreateUserAsync(9307);
        var tokens = await app.Keycloak.GetTokensAsync(user.Email, user.Password, Ct);
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens["access_token"]!.GetValue<string>());

        using var response = await client.DeleteAsync(Route, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await app.Keycloak.Admin.FindUserIdAsync(user.Email, Ct)).ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteAccount_WhenKeycloakCannotDeleteTheUser_ShouldReturnBadGatewayAndKeepTheData()
    {
        app.Clock.Realign();
        var (user, subject) = await CreateUserAsync(9308);
        var resume = await SeedResumeAsync(subject);
        var (client, _) = await app.SignInAsync(user, ct: Ct);
        using var _client = client;
        app.KeycloakAdminBackChannel.Replace = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError);

        try
        {
            using var response = await client.DeleteAsync(Route, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        }
        finally
        {
            app.KeycloakAdminBackChannel.Replace = null;
        }

        (await app.Keycloak.Admin.FindUserIdAsync(user.Email, Ct)).ShouldBe(subject);
        (await CountResumesAsync(resume)).ShouldBe(1);
        (await CountSessionsAsync(subject)).ShouldBe(1);
        (await CountPendingErasuresAsync(subject)).ShouldBe(1, "a failed call may still have deleted the user, so the sweep decides");
        using var stillSignedIn = await client.GetAsync("api/v1/auth/user", Ct);
        stillSignedIn.StatusCode.ShouldBe(HttpStatusCode.OK);
        await RemovePendingErasureAsync(subject);
    }

    [Fact]
    public async Task DeleteAccount_WhenTheErasureFails_ShouldStillSucceedAndKeepThePendingRecord()
    {
        app.Clock.Realign();
        var (user, subject) = await CreateUserAsync(9309);
        var resume = await SeedResumeAsync(subject);
        var (client, _) = await app.SignInAsync(user, ct: Ct);
        using var _client = client;
        var (otherDevice, _) = await app.SignInAsync(user, ct: Ct);
        using var _otherDevice = otherDevice;
        FailingUserDeletedHandler.FailFor.Add(subject);

        try
        {
            using var response = await client.DeleteAsync(Route, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }
        finally
        {
            FailingUserDeletedHandler.FailFor.Remove(subject);
        }

        (await app.Keycloak.Admin.FindUserIdAsync(user.Email, Ct)).ShouldBeNull();
        (await CountSessionsAsync(subject)).ShouldBe(0);
        (await CountPendingErasuresAsync(subject)).ShouldBe(1, "the unfinished erasure stays recorded for the sweep");
        (await CountResumesAsync(resume)).ShouldBe(1);

        await RemovePendingErasureAsync(subject);
    }

    [Fact]
    public async Task Sweep_ForADeletedUserOlderThanTenMinutes_ShouldEraseTheDataAndForgetTheRecord()
    {
        app.Clock.Realign();
        var (user, subject) = await CreateUserAsync(9310);
        var resume = await SeedResumeAsync(subject);
        var (client, _) = await app.SignInAsync(user, ct: Ct);
        using var _client = client;
        await DeleteKeycloakUserAsync(subject);
        await AddPendingErasureAsync(subject, TimeSpan.FromMinutes(11));

        await SweepAsync();

        (await CountResumesAsync(resume)).ShouldBe(0);
        (await CountSessionsAsync(subject)).ShouldBe(0);
        (await CountPendingErasuresAsync(subject)).ShouldBe(0);
    }

    [Fact]
    public async Task Sweep_ForAUserWhoStillExists_ShouldForgetTheRecordAndKeepEverything()
    {
        app.Clock.Realign();
        var (user, subject) = await CreateUserAsync(9311);
        var resume = await SeedResumeAsync(subject);
        var (client, _) = await app.SignInAsync(user, ct: Ct);
        using var _client = client;
        await AddPendingErasureAsync(subject, TimeSpan.FromMinutes(11));

        await SweepAsync();

        (await CountPendingErasuresAsync(subject)).ShouldBe(0);
        (await app.Keycloak.Admin.FindUserIdAsync(user.Email, Ct)).ShouldBe(subject);
        (await CountResumesAsync(resume)).ShouldBe(1);
        (await CountSessionsAsync(subject)).ShouldBe(1);
    }

    [Fact]
    public async Task Sweep_ForARecordYoungerThanTenMinutes_ShouldLeaveItAlone()
    {
        app.Clock.Realign();
        var (_, subject) = await CreateUserAsync(9312);
        var resume = await SeedResumeAsync(subject);
        await DeleteKeycloakUserAsync(subject);
        await AddPendingErasureAsync(subject, TimeSpan.FromMinutes(5));

        await SweepAsync();

        (await CountPendingErasuresAsync(subject)).ShouldBe(1);
        (await CountResumesAsync(resume)).ShouldBe(1);
        await RemovePendingErasureAsync(subject);
    }

    private async Task<int> SweepAsync()
    {
        await using var scope = app.Services.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<PendingErasureSweeper>().SweepAsync(Ct);
    }

    private async Task DeleteKeycloakUserAsync(string subject)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var deleted = await scope.ServiceProvider.GetRequiredService<KeycloakAdminClient>().DeleteUserAsync(subject, Ct);
        deleted.IsError.ShouldBeFalse();
    }

    private async Task AddPendingErasureAsync(string subject, TimeSpan age)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        db.PendingAccountErasures.Add(new PendingAccountErasure { Subject = subject, RequestedAt = app.Clock.GetUtcNow() - age });
        await db.SaveChangesAsync(Ct);
    }

    private async Task RemovePendingErasureAsync(string subject)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await db.PendingAccountErasures.Where(e => e.Subject == subject).ExecuteDeleteAsync(Ct);
    }

    private async Task<int> CountPendingErasuresAsync(string subject)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        return await db.PendingAccountErasures.CountAsync(e => e.Subject == subject, Ct);
    }

    private static async Task ShouldRequireReauthenticationAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>(Ct);
        problem!["code"]!.GetValue<string>().ShouldBe("Account.ReauthenticationRequired");
    }

    private async Task<(TestUser User, string Subject)> CreateUserAsync(int seed)
    {
        var user = new TestUserFaker(seed).Generate();
        var subject = await app.Keycloak.Admin.CreateUserAsync(user, ct: Ct);

        return (user, subject);
    }

    private async Task<Guid> SeedResumeAsync(string subject)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ResumesDbContext>();
        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Parse(subject),
            CreatedAt = DateTimeOffset.UtcNow,
            Title = "Account deletion test"
        };
        resume.Experiences.Add(new Experience { Id = Guid.NewGuid(), JobTitle = "Engineer", Employer = "Acme" });
        db.Resumes.Add(resume);
        await db.SaveChangesAsync(Ct);

        return resume.Id;
    }

    private async Task<int> CountResumesAsync(params Guid[] ids)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ResumesDbContext>();

        return await db.Resumes.CountAsync(r => ids.Contains(r.Id), Ct);
    }

    private async Task<int> CountExperiencesAsync(params Guid[] resumeIds)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ResumesDbContext>();

        return await db.Experiences.CountAsync(e => resumeIds.Contains(e.ResumeId), Ct);
    }

    private async Task<int> CountSessionsAsync(string subject)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        return await db.Sessions.CountAsync(s => s.Subject == subject, Ct);
    }
}
