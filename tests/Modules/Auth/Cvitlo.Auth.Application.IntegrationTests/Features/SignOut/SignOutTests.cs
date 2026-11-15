using System.Net;
using Cvitlo.Common.Testing.Data;
using Shouldly;

namespace Cvitlo.Auth.Application.IntegrationTests.Features.SignOut;

[Collection<CollectionAuth>]
public class SignOutTests(WebApp app)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Logout_SignedIn_ShouldEndTheWebSessionAndTheKeycloakSession()
    {
        // A fresh user: the empty-sessions assertion fails if another test left a session on a shared one.
        var user = new TestUserFaker(5601).Generate();
        await app.Keycloak.Admin.CreateUserAsync(user, ct: TestContext.Current.CancellationToken);
        var (client, _) = await app.SignInAsync(user, ct: TestContext.Current.CancellationToken);
        using var _client = client;
        var userId = (await app.Keycloak.Admin.FindUserIdAsync(user.Email, TestContext.Current.CancellationToken))!;
        (await app.Keycloak.Admin.GetUserSessionsAsync(userId, TestContext.Current.CancellationToken)).Count.ShouldBeGreaterThan(0);

        using var logout = await client.PostAsync("api/v1/auth/logout", null, TestContext.Current.CancellationToken);

        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await app.Keycloak.Admin.GetUserSessionsAsync(userId, TestContext.Current.CancellationToken)).ShouldBeEmpty();
        using var after = await client.GetAsync("api/v1/auth/user", TestContext.Current.CancellationToken);
        after.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_CopiedCookie_ShouldNotRevive()
    {
        var (client, callback) = await app.SignInAsync(app.Users[0], ct: TestContext.Current.CancellationToken);
        using var _client = client;
        var stolen = callback.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("__Host-cvitlo=", StringComparison.Ordinal)).Split(';')[0];

        using var other = app.CreateClient(new FastEndpoints.Testing.ClientOptions { BaseAddress = WebApp.AppOrigin });
        using var control = new HttpRequestMessage(HttpMethod.Get, "api/v1/auth/user");
        control.Headers.Add("Cookie", stolen);
        control.Headers.Add("X-CSRF", "1");
        using var before = await other.SendAsync(control, TestContext.Current.CancellationToken);
        before.StatusCode.ShouldBe(HttpStatusCode.OK, "the copied cookie works until the logout");

        using var logout = await client.PostAsync("api/v1/auth/logout", null, TestContext.Current.CancellationToken);
        using var replay = new HttpRequestMessage(HttpMethod.Get, "api/v1/auth/user");
        replay.Headers.Add("Cookie", stolen);
        replay.Headers.Add("X-CSRF", "1");
        using var response = await other.SendAsync(replay, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, "the session lives on the server, so a copied cookie dies with it");
    }

    [Fact]
    public async Task Logout_NoCsrfHeader_ShouldBeForbidden()
    {
        var (client, _) = await app.SignInAsync(app.Users[1], ct: TestContext.Current.CancellationToken);
        using var _client = client;
        client.DefaultRequestHeaders.Remove("X-CSRF");

        using var logout = await client.PostAsync("api/v1/auth/logout", null, TestContext.Current.CancellationToken);

        logout.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Logout_NoSession_ShouldReturnNoContent()
    {
        using var client = app.CreateBrowserClient();

        using var logout = await client.PostAsync("api/v1/auth/logout", null, Ct);

        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact(Skip = "#151: KeycloakTokenClient.LogoutAsync catches only HttpRequestException, so a failing Keycloak (retries then the 5 s timeout) ends in an unhandled TaskCanceledException, answer 499, and the web session is not ended")]
    public async Task Logout_KeycloakFailing_ShouldStillEndTheWebSession()
    {
        var (client, _) = await app.SignInAsync(await CreateUserAsync(5202), ct: Ct);
        using var _client = client;

        app.KeycloakBackChannel.Replace = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError);
        HttpResponseMessage logout;
        try
        {
            logout = await client.PostAsync("api/v1/auth/logout", null, Ct);
        }
        finally
        {
            app.KeycloakBackChannel.Replace = null;
        }

        using var _logout = logout;
        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var after = await client.GetAsync("api/v1/auth/user", Ct);
        after.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(Skip = "#155: a Bearer-prefixed Authorization header switches off the CSRF check, so logout with the session cookie and no X-CSRF answers 204 and ends the session")]
    public async Task Logout_SessionCookieWithABearerHeaderAndNoCsrfHeader_ShouldNotEndTheSession()
    {
        var (client, _) = await app.SignInAsync(await CreateUserAsync(5201), ct: Ct);
        using var _client = client;
        client.DefaultRequestHeaders.Remove("X-CSRF");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "x");

        using var logout = await client.PostAsync("api/v1/auth/logout", null, Ct);

        using var check = new HttpRequestMessage(HttpMethod.Get, "api/v1/auth/user");
        check.Headers.Add("X-CSRF", "1");
        client.DefaultRequestHeaders.Authorization = null;
        using var after = await client.SendAsync(check, Ct);
        after.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"a request that carries the cookie must send the CSRF header whatever else it sends (logout answered {(int)logout.StatusCode})");
    }

    private async Task<TestUser> CreateUserAsync(int seed)
    {
        var user = new TestUserFaker(seed).Generate();
        await app.Keycloak.Admin.CreateUserAsync(user, ct: Ct);

        return user;
    }
}
