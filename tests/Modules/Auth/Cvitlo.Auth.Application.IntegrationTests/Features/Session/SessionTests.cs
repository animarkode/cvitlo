using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Cvitlo.Common.Testing.Containers;
using Cvitlo.Common.Testing.Http;
using Shouldly;

namespace Cvitlo.Auth.Application.IntegrationTests.Features.Session;

[Collection<CollectionAuth>]
public class SessionTests(WebApp app)
{
    [Fact]
    public async Task User_SignedIn_ShouldReturnTheProfileFromKeycloak()
    {
        var user = app.Users[0];
        var (client, _) = await app.SignInAsync(user, ct: TestContext.Current.CancellationToken);
        using var _client = client;

        var profile = await client.GetFromJsonAsync<JsonObject>("api/v1/auth/user", TestContext.Current.CancellationToken);

        profile!["email"]!.GetValue<string>().ShouldBe(user.Email, StringCompareShould.IgnoreCase);
        profile["firstName"]!.GetValue<string>().ShouldBe(user.FirstName);
        profile["lastName"]!.GetValue<string>().ShouldBe(user.LastName);
        profile["id"]!.GetValue<string>().ShouldBe(await app.Keycloak.Admin.FindUserIdAsync(user.Email, TestContext.Current.CancellationToken));
        profile["roles"]!.AsArray().Select(r => r!.GetValue<string>()).ShouldContain("regular", StringComparer.Ordinal);
    }

    [Fact]
    public async Task User_NoSession_ShouldReturnUnauthorized()
    {
        using var client = app.CreateBrowserClient();

        using var response = await client.GetAsync("api/v1/auth/user", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CookieRequest_NoCsrfHeader_ShouldBeForbidden()
    {
        var (client, _) = await app.SignInAsync(app.Users[0], ct: TestContext.Current.CancellationToken);
        using var _client = client;
        client.DefaultRequestHeaders.Remove("X-CSRF");

        using var response = await client.GetAsync("api/v1/auth/user", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CookieRequest_NonBearerAuthorizationHeader_ShouldStillNeedTheCsrfHeader()
    {
        var (client, _) = await app.SignInAsync(app.Users[0], ct: TestContext.Current.CancellationToken);
        using var _client = client;
        client.DefaultRequestHeaders.Remove("X-CSRF");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", "eDp5");

        using var response = await client.GetAsync("api/v1/auth/user", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden,
            "only a bearer token switches the request off the session; anything else still rides on the cookie");
    }

    [Fact]
    public async Task Refresh_ExpiredAccessToken_ShouldBeRefreshedOnTheServer()
    {
        var (client, _) = await app.SignInAsync(app.Users[1], ct: TestContext.Current.CancellationToken);
        using var _client = client;

        try
        {
            app.Clock.Advance(TimeSpan.FromMinutes(6));
            using var response = await client.GetAsync("api/v1/auth/user", TestContext.Current.CancellationToken);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
        finally
        {
            app.Clock.Realign();
        }
    }

    [Fact]
    public async Task Refresh_ParallelRequestsNearExpiry_ShouldRefreshOnceAndKeepTheSession()
    {
        var (client, _) = await app.SignInAsync(app.Users[2], ct: TestContext.Current.CancellationToken);
        using var _client = client;

        try
        {
            app.Clock.Advance(TimeSpan.FromMinutes(6));
            var responses = await Task.WhenAll(Enumerable.Range(0, 6)
                .Select(_ => client.GetAsync("api/v1/auth/user", TestContext.Current.CancellationToken)));

            responses.Select(r => r.StatusCode).ShouldAllBe(status => status == HttpStatusCode.OK,
                "a refresh token works once, so concurrent refreshes would sign the user out");
            using var after = await client.GetAsync("api/v1/auth/user", TestContext.Current.CancellationToken);
            after.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
        finally
        {
            app.Clock.Realign();
        }
    }

    [Fact]
    public async Task Refresh_SessionEndedInKeycloak_ShouldEndTheWebSession()
    {
        var user = app.Users[3];
        var (client, _) = await app.SignInAsync(user, ct: TestContext.Current.CancellationToken);
        using var _client = client;
        var userId = (await app.Keycloak.Admin.FindUserIdAsync(user.Email, TestContext.Current.CancellationToken))!;

        await app.Keycloak.Admin.SignOutUserAsync(userId, TestContext.Current.CancellationToken);
        try
        {
            app.Clock.Advance(TimeSpan.FromMinutes(6));
            using var response = await client.GetAsync("api/v1/auth/user", TestContext.Current.CancellationToken);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
        finally
        {
            app.Clock.Realign();
        }
    }

    [Fact]
    public async Task CookieRequest_GarbageBearerHeader_ShouldBeRefusedAsMixedCredentials()
    {
        var (client, _) = await app.SignInAsync(app.Users[0], ct: TestContext.Current.CancellationToken);
        using var _client = client;
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "x");

        using var response = await client.GetAsync("api/v1/auth/account/profile", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadProblemCodeAsync(TestContext.Current.CancellationToken)).ShouldBe("Auth.MixedCredentials");
    }

    [Fact]
    public async Task SessionCookie_TamperedValue_ShouldReturnUnauthorized()
    {
        var (client, callback) = await app.SignInAsync(app.Users[0], ct: TestContext.Current.CancellationToken);
        using var _client = client;
        var cookie = callback.Headers.GetValues("Set-Cookie")
            .Single(c => c.StartsWith("__Host-cvitlo=", StringComparison.Ordinal)).Split(';')[0];
        var middle = cookie.Length / 2;
        var tampered = cookie[..middle] + (cookie[middle] == 'A' ? 'B' : 'A') + cookie[(middle + 1)..];

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/auth/user");
        request.Headers.Add("Cookie", tampered);
        request.Headers.Add("X-CSRF", "1");
        using var other = app.CreateClient(new FastEndpoints.Testing.ClientOptions { BaseAddress = WebApp.AppOrigin });
        using var response = await other.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        using var control = new HttpRequestMessage(HttpMethod.Get, "api/v1/auth/user");
        control.Headers.Add("Cookie", cookie);
        control.Headers.Add("X-CSRF", "1");
        using var genuine = await other.SendAsync(control, TestContext.Current.CancellationToken);
        genuine.StatusCode.ShouldBe(HttpStatusCode.OK, "the same path accepts the untampered cookie");
    }

    [Fact]
    public async Task Session_PastItsThirtyDayLifetime_ShouldReturnUnauthorized()
    {
        var (client, _) = await app.SignInAsync(app.Users[0], ct: TestContext.Current.CancellationToken);
        using var _client = client;

        try
        {
            app.Clock.Advance(TimeSpan.FromDays(31));
            using var response = await client.GetAsync("api/v1/auth/user", TestContext.Current.CancellationToken);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
        finally
        {
            app.Clock.Realign();
        }
    }

    [Fact]
    public async Task Refresh_KeycloakReturnsNoRefreshToken_ShouldKeepThePreviousOne()
    {
        var user = new Cvitlo.Common.Testing.Data.TestUserFaker(5401).Generate();
        await app.Keycloak.Admin.CreateUserAsync(user, ct: TestContext.Current.CancellationToken);
        var (client, _) = await app.SignInAsync(user, ct: TestContext.Current.CancellationToken);
        using var _client = client;
        var fresh = await app.Keycloak.GetTokensAsync(user.Email, user.Password, TestContext.Current.CancellationToken);
        var answered = false;

        try
        {
            // Keycloak is not asked, so the session's refresh token stays unspent for the next real refresh.
            app.Clock.Advance(TimeSpan.FromMinutes(6));
            app.KeycloakBackChannel.Replace = _ =>
            {
                answered = true;
                var body = new JsonObject { ["access_token"] = fresh["access_token"]!.GetValue<string>(), ["expires_in"] = 300 };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body.ToJsonString(), System.Text.Encoding.UTF8, "application/json")
                };
            };
            using var first = await client.GetAsync("api/v1/auth/user", TestContext.Current.CancellationToken);
            app.KeycloakBackChannel.Replace = null;

            first.StatusCode.ShouldBe(HttpStatusCode.OK);
            answered.ShouldBeTrue("the first request must have gone through the refresh");

            app.Clock.Advance(TimeSpan.FromMinutes(6));
            using var second = await client.GetAsync("api/v1/auth/user", TestContext.Current.CancellationToken);
            second.StatusCode.ShouldBe(HttpStatusCode.OK, "the previous refresh token is still the one in the session");
        }
        finally
        {
            app.KeycloakBackChannel.Replace = null;
            app.Clock.Realign();
        }
    }
}
