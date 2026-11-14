using System.Net;
using Cvitlo.Common.Testing.Data;
using Shouldly;

namespace Cvitlo.Auth.Application.IntegrationTests.Features.BackchannelLogout;

[Collection<CollectionAuth>]
public class BackchannelLogoutTests(WebApp app)
{
    [Fact]
    public async Task BackchannelLogout_SessionEndedInKeycloak_ShouldEndTheWebSessionImmediately()
    {
        // A user of its own, so the one Keycloak session deleted below is this browser's.
        var user = new TestUserFaker(4242).Generate();
        await app.Keycloak.Admin.CreateUserAsync(user, ct: TestContext.Current.CancellationToken);
        var (client, _) = await app.SignInAsync(user, ct: TestContext.Current.CancellationToken);
        using var _client = client;
        var userId = (await app.Keycloak.Admin.FindUserIdAsync(user.Email, TestContext.Current.CancellationToken))!;
        var sessions = await app.Keycloak.Admin.GetUserSessionsAsync(userId, TestContext.Current.CancellationToken);

        await app.Keycloak.Admin.DeleteSessionAsync(sessions[0]!["id"]!.GetValue<string>(), TestContext.Current.CancellationToken);

        // Keycloak delivers the logout token asynchronously; the clock is never advanced, so only the logout
        // token (not a failed refresh) can end the web session here.
        var status = HttpStatusCode.OK;
        for (var attempt = 0; attempt < 50 && status != HttpStatusCode.Unauthorized; attempt++)
        {
            await Task.Delay(200, TestContext.Current.CancellationToken);
            using var response = await client.GetAsync("api/v1/auth/user", TestContext.Current.CancellationToken);
            status = response.StatusCode;
        }

        status.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task BackchannelLogout_ForgedLogoutToken_ShouldReturnBadRequest()
    {
        using var client = app.CreateClient(new FastEndpoints.Testing.ClientOptions { BaseAddress = WebApp.AppOrigin });
        const string forged = "eyJhbGciOiJub25lIn0.eyJzaWQiOiJ4In0.";

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal) { ["logout_token"] = forged });
        using var response = await client.PostAsync("api/v1/auth/backchannel-logout", content,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BackchannelLogout_NoLogoutToken_ShouldReturnBadRequest()
    {
        using var client = app.CreateClient(new FastEndpoints.Testing.ClientOptions { BaseAddress = WebApp.AppOrigin });

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal));
        using var response = await client.PostAsync("api/v1/auth/backchannel-logout", content, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BackchannelLogout_JsonBody_ShouldReturnUnsupportedMediaType()
    {
        using var client = app.CreateClient(new FastEndpoints.Testing.ClientOptions { BaseAddress = WebApp.AppOrigin });

        using var content = new StringContent("""{"logout_token":"eyJhbGciOiJub25lIn0.eyJzaWQiOiJ4In0."}""", System.Text.Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("api/v1/auth/backchannel-logout", content, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task BackchannelLogout_GarbageTokenWithoutCsrfHeader_ShouldReturnBadRequestNotForbidden()
    {
        var (client, _) = await app.SignInAsync(app.Users[0], ct: TestContext.Current.CancellationToken);
        using var _client = client;
        client.DefaultRequestHeaders.Remove("X-CSRF");

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal) { ["logout_token"] = "garbage" });
        using var response = await client.PostAsync("api/v1/auth/backchannel-logout", content, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "Keycloak sends no CSRF header; the token check, not the CSRF check, answers");
    }
}
