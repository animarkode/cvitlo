using System.Net;
using System.Web;
using Cvitlo.Auth.Infrastructure.Database;
using Cvitlo.Common.Testing.Data;
using Cvitlo.Common.Testing.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Cvitlo.Auth.Application.IntegrationTests.Features.AccountAction;

[Collection<CollectionAuth>]
public class AccountActionTests(WebApp app)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AccountAction_NoSession_ShouldReturnUnauthorized()
    {
        using var client = app.CreateBrowserClient();

        using var response = await client.GetAsync("api/v1/auth/account-action/password", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("delete")]
    [InlineData("UPDATE_PASSWORD")]
    [InlineData("idp_link:google")]
    public async Task AccountAction_UnknownAction_ShouldReturnNotFound(string action)
    {
        var (client, _) = await app.SignInAsync(app.Users[0], ct: Ct);
        using var _client = client;

        using var response = await client.GetAsync($"api/v1/auth/account-action/{Uri.EscapeDataString(action)}", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("password", "UPDATE_PASSWORD")]
    [InlineData("passkey", "webauthn-register-passwordless")]
    [InlineData("totp", "CONFIGURE_TOTP")]
    [InlineData("email", "UPDATE_EMAIL")]
    [InlineData("link-google", "idp_link:google")]
    [InlineData("link-github", "idp_link:github")]
    public async Task AccountAction_AllowListedAction_ShouldRedirectToKeycloakWithTheMappedAction(string action, string kcAction)
    {
        var (client, _) = await app.SignInAsync(app.Users[0], ct: Ct);
        using var _client = client;

        using var response = await client.GetAsync($"api/v1/auth/account-action/{action}", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var location = response.Headers.Location!;
        location.GetLeftPart(UriPartial.Path).ShouldBe($"{app.Keycloak.Issuer}/protocol/openid-connect/auth");
        HttpUtility.ParseQueryString(location.Query)["kc_action"].ShouldBe(kcAction);
    }

    [Fact]
    public async Task AccountAction_SignedIn_ShouldSendTheStoredIdTokenAsHint()
    {
        var (client, _) = await app.SignInAsync(app.Users[0], ct: Ct);
        using var _client = client;

        using var response = await client.GetAsync("api/v1/auth/account-action/password", Ct);

        var hint = HttpUtility.ParseQueryString(response.Headers.Location!.Query)["id_token_hint"];
        hint.ShouldNotBeNullOrWhiteSpace();
        hint.Split('.').Length.ShouldBe(3, "the hint is the user's ID token (a JWT)");
    }

    [Fact]
    public async Task AccountAction_ExtraKcActionInTheQuery_ShouldNotForwardIt()
    {
        var (client, _) = await app.SignInAsync(app.Users[0], ct: Ct);
        using var _client = client;

        using var response = await client.GetAsync("api/v1/auth/account-action/password?kc_action=delete_account&prompt=none", Ct);

        var query = HttpUtility.ParseQueryString(response.Headers.Location!.Query);
        query.GetValues("kc_action").ShouldBe(["UPDATE_PASSWORD"]);
        query["prompt"].ShouldBeNull();
    }

    [Fact]
    public async Task AccountAction_NoCsrfHeader_ShouldStillBeAllowedAsANavigation()
    {
        var (client, _) = await app.SignInAsync(app.Users[0], ct: Ct);
        using var _client = client;
        client.DefaultRequestHeaders.Remove("X-CSRF");

        using var response = await client.GetAsync("api/v1/auth/account-action/password", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task AccountAction_NestedPath_ShouldStillNeedTheCsrfHeader()
    {
        var (client, _) = await app.SignInAsync(app.Users[0], ct: Ct);
        using var _client = client;
        client.DefaultRequestHeaders.Remove("X-CSRF");

        using var response = await client.GetAsync("api/v1/auth/account-action/password/extra", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AccountAction_CancelledOnKeycloak_ShouldReturnToTheAccountPageWithOneSession()
    {
        var user = app.Users[4];
        var subject = await app.Keycloak.Admin.FindUserIdAsync(user.Email, Ct);
        using var client = app.CreateBrowserClient();

        // One driver for both steps, so its Keycloak cookies hold the single sign-on session of the sign-in.
        using var driver = new KeycloakLoginDriver(client);
        using var signedIn = await driver.SignInAsync(user.Email, user.Password, ct: Ct);
        var page = await driver.StartAsync("api/v1/auth/account-action/password", Ct);
        using var cancelled = await driver.SubmitAsync(page, "kc-passwd-update-form", new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["cancel-aia"] = "true"
        }, Ct);
        using var callback = await driver.FinishAtApiAsync(cancelled, Ct);

        callback.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        callback.Headers.Location!.OriginalString.ShouldBe("/account/signing-in?action=password&status=cancelled");
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        (await db.Sessions.CountAsync(s => s.Subject == subject, Ct)).ShouldBe(1,
            "the session the action replaced must not linger next to the new one");
        using var profile = await client.GetAsync("api/v1/auth/user", Ct);
        profile.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // Keycloak locks the re-authentication form to the signed-in user (the name is read-only), so another user's
    // credentials never reach the callback; this pins that outcome, and that nothing changed on the API side.
    [Fact]
    public async Task AccountAction_CredentialsOfAnotherUser_ShouldBeRefusedByKeycloakAndKeepTheOriginalSession()
    {
        var owner = app.Users[3];
        var intruder = app.Users[2];
        var ownerId = (await app.Keycloak.Admin.FindUserIdAsync(owner.Email, Ct))!;
        var intruderId = (await app.Keycloak.Admin.FindUserIdAsync(intruder.Email, Ct))!;
        using var client = app.CreateBrowserClient();
        using var driver = new KeycloakLoginDriver(client);
        using var signedIn = await driver.SignInAsync(owner.Email, owner.Password, ct: Ct);
        var ownerBefore = await SessionIdsAsync(ownerId);
        var intruderBefore = await SessionIdsAsync(intruderId);

        var page = await driver.StartAsync("api/v1/auth/account-action/reauthenticate", Ct);
        using var submitted = await driver.SubmitAsync(page, "kc-form-login", Credentials(intruder), Ct);

        submitted.StatusCode.ShouldBe(HttpStatusCode.OK, "Keycloak shows its error on its own page and sends no code");
        submitted.Headers.Location.ShouldBeNull();
        (await submitted.Content.ReadAsStringAsync(Ct)).ShouldContain("kc-attempted-username");
        using var profile = await client.GetAsync("api/v1/auth/user", Ct);
        profile.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await profile.Content.ReadAsStringAsync(Ct)).ShouldContain(owner.Email, Case.Insensitive);
        (await SessionIdsAsync(ownerId)).ShouldBe(ownerBefore, ignoreOrder: true);
        (await SessionIdsAsync(intruderId)).ShouldBe(intruderBefore, ignoreOrder: true);
    }

    [Fact]
    public async Task AccountAction_Reauthenticate_ShouldReplaceTheSessionAndReportSuccess()
    {
        var user = app.Users[3];
        var userId = (await app.Keycloak.Admin.FindUserIdAsync(user.Email, Ct))!;
        using var client = app.CreateBrowserClient();
        using var driver = new KeycloakLoginDriver(client);
        using var signedIn = await driver.SignInAsync(user.Email, user.Password, ct: Ct);
        var before = await SessionIdsAsync(userId);

        var page = await driver.StartAsync("api/v1/auth/account-action/reauthenticate", Ct);
        using var submitted = await driver.SubmitAsync(page, "kc-form-login", Credentials(user), Ct);
        using var callback = await driver.FinishAtApiAsync(submitted, Ct);

        callback.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        callback.Headers.Location!.OriginalString.ShouldEndWith("action=reauthenticate&status=success");
        var after = await SessionIdsAsync(userId);
        after.Count.ShouldBe(before.Count, "the new session replaces the old one");
        after.Except(before, StringComparer.Ordinal).ShouldHaveSingleItem();
        before.Except(after, StringComparer.Ordinal).ShouldHaveSingleItem();
        using var profile = await client.GetAsync("api/v1/auth/user", Ct);
        profile.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static Dictionary<string, string> Credentials(TestUser user)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["username"] = user.Email,
            ["password"] = user.Password,
            ["credentialId"] = string.Empty
        };
    }

    private async Task<List<string>> SessionIdsAsync(string subject)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        return await db.Sessions.Where(s => s.Subject == subject).Select(s => s.Id).ToListAsync(Ct);
    }
}
