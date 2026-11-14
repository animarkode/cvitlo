using System.Net;
using System.Web;
using Cvitlo.Common.Testing.Containers;
using Cvitlo.Common.Testing.Data;
using Cvitlo.Common.Testing.Http;
using Shouldly;

namespace Cvitlo.Auth.Application.IntegrationTests.Features.SignIn;

[Collection<CollectionAuth>]
public class SignInTests(WebApp app)
{
    [Fact]
    public async Task Login_ShouldRedirectToKeycloakWithCodeFlowPkceAndState()
    {
        using var client = app.CreateBrowserClient();

        using var response = await client.GetAsync("api/v1/auth/login", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var location = response.Headers.Location!;
        location.GetLeftPart(UriPartial.Path).ShouldBe($"{app.Keycloak.Issuer}/protocol/openid-connect/auth");
        var query = HttpUtility.ParseQueryString(location.Query);
        query["client_id"].ShouldBe(CvitloKeycloak.ClientId);
        query["response_type"].ShouldBe("code");
        query["code_challenge_method"].ShouldBe("S256");
        query["code_challenge"].ShouldNotBeNullOrWhiteSpace();
        query["state"].ShouldNotBeNullOrWhiteSpace();
        query["redirect_uri"].ShouldBe($"{WebApp.AppOrigin.ToString().TrimEnd('/')}/api/v1/auth/callback");
        query["scope"]!.Split(' ').ShouldNotContain("offline_access", StringComparer.Ordinal);
    }

    [Fact]
    public async Task Register_ShouldAskKeycloakForTheRegistrationPage()
    {
        using var client = app.CreateBrowserClient();

        using var response = await client.GetAsync("api/v1/auth/register", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        HttpUtility.ParseQueryString(response.Headers.Location!.Query)["prompt"].ShouldBe("create");
    }

    // The registration page has no password field: whoever submits it has not yet proved they own the
    // address, so Keycloak must send the verification link before it lets them set a password.
    [Fact]
    public async Task Register_ShouldVerifyTheEmailBeforeAskingForAPassword()
    {
        var user = new TestUserFaker(9201).Generate();
        using var client = app.CreateBrowserClient();
        using var driver = new KeycloakLoginDriver(client);
        var registerPage = await driver.StartAsync("api/v1/auth/register", TestContext.Current.CancellationToken);

        using var submitted = await driver.SubmitAsync(registerPage, "kc-register-form", new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["email"] = user.Email,
            ["firstName"] = user.FirstName,
            ["lastName"] = user.LastName
        }, TestContext.Current.CancellationToken);
        var (page, apiResponse) = await driver.FollowAsync(submitted, TestContext.Current.CancellationToken);

        apiResponse.ShouldBeNull("an unverified registration must not sign anyone in");
        page.ShouldNotBeNull();
        page.ShouldNotContain("<!-- template: login-update-password.ftl -->", customMessage: "no password before the e-mail is verified");
        page.ShouldContain("<!-- template: login-verify-email.ftl -->", customMessage: "the next page is the verify-e-mail page");

        var mail = await app.Keycloak.GetLatestMailTextAsync(user.Email, TestContext.Current.CancellationToken);
        var link = mail.Split('\n')
            .Select(line => line.Trim())
            .First(line => line.Contains("/login-actions/action-token", StringComparison.Ordinal));
        var afterLink = await driver.GetPageAsync(new Uri(link), TestContext.Current.CancellationToken);

        afterLink.ShouldContain("<!-- template: login-update-password.ftl -->", customMessage: "the password is set once the link is followed");
    }

    [Fact]
    public async Task SignIn_ShouldIssueAnHttpOnlySecureStrictSessionCookie()
    {
        var (client, callback) = await app.SignInAsync(app.Users[0], ct: TestContext.Current.CancellationToken);
        using var _ = client;

        callback.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var cookie = callback.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("__Host-cvitlo=", StringComparison.Ordinal));
        var attributes = cookie.Split(';', StringSplitOptions.TrimEntries).Select(a => a.ToLowerInvariant()).ToList();
        attributes.ShouldContain("httponly", StringComparer.Ordinal);
        attributes.ShouldContain("secure", StringComparer.Ordinal);
        attributes.ShouldContain("samesite=strict", StringComparer.Ordinal);
        attributes.ShouldContain("path=/", StringComparer.Ordinal);
        attributes.ShouldNotContain(a => a.StartsWith("domain=", StringComparison.Ordinal));
        attributes.ShouldNotContain(a => a.StartsWith("expires=", StringComparison.Ordinal),
            "without remember me the session must end with the browser");
    }

    [Fact]
    public async Task SignIn_WithRememberMe_ShouldIssueAPersistentCookie()
    {
        var (client, callback) = await app.SignInAsync(app.Users[1], rememberMe: true, ct: TestContext.Current.CancellationToken);
        using var _ = client;

        var cookie = callback.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("__Host-cvitlo=", StringComparison.Ordinal));
        cookie.ToLowerInvariant().ShouldContain("expires=");
    }

    [Fact]
    public async Task SignIn_ShouldReturnToTheRequestedLocalPage()
    {
        var (client, callback) = await app.SignInAsync(app.Users[0], returnUrl: "/resumes/42", ct: TestContext.Current.CancellationToken);
        using var _ = client;

        callback.Headers.Location!.OriginalString.ShouldBe("/resumes/42");
    }

    [Theory]
    [InlineData("https://evil.example/steal")]
    [InlineData("//evil.example/steal")]
    [InlineData("/\\evil.example/steal")]
    [InlineData("javascript:alert(1)")]
    public async Task SignIn_WithForeignReturnUrl_ShouldReturnToTheAppRoot(string returnUrl)
    {
        var (client, callback) = await app.SignInAsync(app.Users[0], returnUrl: returnUrl, ct: TestContext.Current.CancellationToken);
        using var _ = client;

        callback.Headers.Location!.OriginalString.ShouldBe("/");
    }

    [Fact]
    public async Task SignIn_ShouldNeverPutTokensInAResponseTheBrowserCanRead()
    {
        var (client, callback) = await app.SignInAsync(app.Users[2], ct: TestContext.Current.CancellationToken);
        using var _ = client;

        var body = await callback.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldNotContain("eyJ");
        callback.Headers.Location!.OriginalString.ShouldNotContain("token");
        using var user = await client.GetAsync("api/v1/auth/user", TestContext.Current.CancellationToken);
        (await user.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldNotContain("eyJ");
    }

    [Fact]
    public async Task SignIn_WithWrongPassword_ShouldStayOnKeycloak()
    {
        using var client = app.CreateBrowserClient();
        using var driver = new KeycloakLoginDriver(client);
        var page = await driver.StartAsync("api/v1/auth/login", TestContext.Current.CancellationToken);

        using var response = await driver.SubmitAsync(page, "kc-form-login", new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["username"] = app.Users[3].Email,
            ["password"] = "not-the-password",
            ["credentialId"] = string.Empty
        }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK, "Keycloak shows its error on its own page");
        response.Headers.Location.ShouldBeNull();
    }
}
