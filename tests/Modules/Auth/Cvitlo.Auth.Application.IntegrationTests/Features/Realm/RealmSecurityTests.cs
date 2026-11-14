using System.Net;
using System.Text.Json.Nodes;
using Cvitlo.Common.Testing.Containers;
using Cvitlo.Common.Testing.Data;
using Cvitlo.Common.Testing.Http;
using Shouldly;

namespace Cvitlo.Auth.Application.IntegrationTests.Features.Realm;

/// <summary>
/// Pins the security settings of the realm as applied from <c>infra/keycloak/config/cvitlo.json</c>, so a
/// config change that weakens one of them fails here rather than in production.
/// </summary>
[Collection<CollectionAuth>]
public class RealmSecurityTests(WebApp app)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Realm_ShouldUseEmailAsLoginAndRequireAVerifiedEmail()
    {
        var realm = await app.Keycloak.Admin.GetRealmAsync(Ct);

        realm["registrationEmailAsUsername"]!.GetValue<bool>().ShouldBeTrue();
        realm["verifyEmail"]!.GetValue<bool>().ShouldBeTrue();
        realm["duplicateEmailsAllowed"]!.GetValue<bool>().ShouldBeFalse();
        realm["editUsernameAllowed"]!.GetValue<bool>().ShouldBeFalse();
    }

    [Fact]
    public async Task Realm_ShouldLockOutTemporarilyAndEnforceThePasswordPolicy()
    {
        var realm = await app.Keycloak.Admin.GetRealmAsync(Ct);

        realm["bruteForceProtected"]!.GetValue<bool>().ShouldBeTrue();
        realm["permanentLockout"]!.GetValue<bool>().ShouldBeFalse("anyone can trigger a lockout, so it must end by itself");
        realm["failureFactor"]!.GetValue<int>().ShouldBe(5);
        realm["passwordPolicy"]!.GetValue<string>().ShouldContain("length(8)");
        realm["passwordPolicy"]!.GetValue<string>().ShouldContain("notEmail");
    }

    [Fact]
    public async Task Realm_ShouldRotateRefreshTokensAndBoundOfflineSessions()
    {
        var realm = await app.Keycloak.Admin.GetRealmAsync(Ct);

        realm["revokeRefreshToken"]!.GetValue<bool>().ShouldBeTrue();
        realm["refreshTokenMaxReuse"]!.GetValue<int>().ShouldBe(0);
        realm["offlineSessionMaxLifespanEnabled"]!.GetValue<bool>().ShouldBeTrue();
        realm["rememberMe"]!.GetValue<bool>().ShouldBeTrue();
        realm["eventsEnabled"]!.GetValue<bool>().ShouldBeTrue();
    }

    [Fact]
    public async Task WebClient_ShouldBeConfidentialCodeFlowOnlyWithPkceRequired()
    {
        var client = await app.Keycloak.Admin.GetClientAsync(CvitloKeycloak.ClientId, Ct);

        client["publicClient"]!.GetValue<bool>().ShouldBeFalse();
        client["standardFlowEnabled"]!.GetValue<bool>().ShouldBeTrue();
        client["directAccessGrantsEnabled"]!.GetValue<bool>().ShouldBeFalse("the password grant must never come back");
        client["implicitFlowEnabled"]!.GetValue<bool>().ShouldBeFalse();
        client["serviceAccountsEnabled"]!.GetValue<bool>().ShouldBeFalse("the API needs no admin rights in Keycloak");
        client["attributes"]!["pkce.code.challenge.method"]!.GetValue<string>().ShouldBe("S256");
        client["redirectUris"]!.AsArray().Select(u => u!.GetValue<string>())
            .ShouldBe([$"{WebApp.AppOrigin.ToString().TrimEnd('/')}/api/v1/auth/callback"]);
    }

    [Fact]
    public async Task BuiltInClients_ShouldNotOfferAPasswordGrantOrTheRealmAdminConsole()
    {
        var adminCli = await app.Keycloak.Admin.GetClientAsync("admin-cli", Ct);
        var adminConsole = await app.Keycloak.Admin.GetClientAsync("security-admin-console", Ct);
        var accountConsole = await app.Keycloak.Admin.GetClientAsync("account-console", Ct);

        adminCli["directAccessGrantsEnabled"]!.GetValue<bool>().ShouldBeFalse();
        adminConsole["enabled"]!.GetValue<bool>().ShouldBeFalse();
        accountConsole["enabled"]!.GetValue<bool>().ShouldBeFalse("the account is managed in the app, and the console bypasses its checks");
    }

    [Theory]
    [InlineData("account-console")]
    [InlineData("account")]
    public async Task BrowserSignIn_ThroughKeycloakAccountClients_ShouldBeRefused(string clientId)
    {
        // Keycloak refuses either with an error page or by redirecting back with `error=`; following that redirect
        // would land on an unrelated page, so it is read as is.
        using var handler = new HttpClientHandler { AllowAutoRedirect = false, CheckCertificateRevocationList = true };
        using var http = new HttpClient(handler) { BaseAddress = new Uri(app.Keycloak.BaseAddress) };
        var redirectUri = Uri.EscapeDataString($"{app.Keycloak.BaseAddress.TrimEnd('/')}/realms/{CvitloKeycloak.Realm}/account/");

        using var response = await http.GetAsync(
            $"realms/{CvitloKeycloak.Realm}/protocol/openid-connect/auth?client_id={clientId}&response_type=code&scope=openid&redirect_uri={redirectUri}", Ct);
        var body = await response.Content.ReadAsStringAsync(Ct);
        var refusedByRedirect = (int)response.StatusCode is >= 300 and < 400
                                && response.Headers.Location?.ToString().Contains("error=", StringComparison.Ordinal) == true;

        (refusedByRedirect || (int)response.StatusCode >= 400).ShouldBeTrue($"Keycloak answered {(int)response.StatusCode} {response.Headers.Location}");
        body.ShouldNotContain("kc-form-login");
    }

    [Fact]
    public async Task AccountClient_ShouldKeepOnlyTheRolesForTheAccountRestApi()
    {
        var account = await app.Keycloak.Admin.GetClientAsync("account", Ct);

        account["enabled"]!.GetValue<bool>().ShouldBeTrue("its roles back the Account REST API");
        account["standardFlowEnabled"]!.GetValue<bool>().ShouldBeFalse();
        account["implicitFlowEnabled"]!.GetValue<bool>().ShouldBeFalse();
        account["directAccessGrantsEnabled"]!.GetValue<bool>().ShouldBeFalse();
    }

    [Theory]
    [InlineData("UPDATE_PASSWORD")]
    [InlineData("CONFIGURE_TOTP")]
    [InlineData("webauthn-register-passwordless")]
    [InlineData("idp_link")]
    [InlineData("UPDATE_EMAIL")]
    public async Task SensitiveRequiredAction_ShouldBeEnabledAndRequireARecentSignIn(string alias)
    {
        var action = await app.Keycloak.Admin.GetRequiredActionAsync(alias, Ct);

        action["enabled"]!.GetValue<bool>().ShouldBeTrue();
        action["config"]!["max_auth_age"]!.GetValue<string>().ShouldBe("300");
    }

    // Keycloak runs pending required actions lowest priority first. A credential set up before the e-mail
    // is verified would belong to whoever typed the address, not to its owner.
    [Theory]
    [InlineData("UPDATE_PASSWORD")]
    [InlineData("CONFIGURE_TOTP")]
    [InlineData("webauthn-register-passwordless")]
    public async Task VerifyEmail_ShouldRunBeforeSettingUpACredential(string alias)
    {
        var verifyEmail = await app.Keycloak.Admin.GetRequiredActionAsync("VERIFY_EMAIL", Ct);
        var credential = await app.Keycloak.Admin.GetRequiredActionAsync(alias, Ct);

        verifyEmail["enabled"]!.GetValue<bool>().ShouldBeTrue();
        verifyEmail["priority"]!.GetValue<int>().ShouldBeLessThan(credential["priority"]!.GetValue<int>());
    }

    [Fact]
    public async Task DefaultRoles_ShouldNotLetUsersDeleteTheirOwnAccountThroughKeycloak()
    {
        var composites = await app.Keycloak.Admin.GetRealmRoleCompositesAsync("default-roles-cvitlo", Ct);

        composites.Any(r => string.Equals(r!["name"]!.GetValue<string>(), "delete-account", StringComparison.Ordinal))
            .ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAccountRequiredAction_ShouldBeDisabled()
    {
        var action = await app.Keycloak.Admin.GetRequiredActionAsync("delete_account", Ct);

        action["enabled"]!.GetValue<bool>().ShouldBeFalse();
    }

    [Fact]
    public async Task AdminClient_ShouldRefuseThePasswordGrant()
    {
        var user = app.Users[0];
        using var http = new HttpClient { BaseAddress = new Uri(app.Keycloak.BaseAddress) };

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["grant_type"] = "password",
                ["client_id"] = CvitloKeycloak.AdminClientId,
                ["client_secret"] = CvitloKeycloak.AdminClientSecret,
                ["username"] = user.Email,
                ["password"] = user.Password
            });
        using var response = await http.PostAsync($"realms/{CvitloKeycloak.Realm}/protocol/openid-connect/token", content, Ct);

        response.IsSuccessStatusCode.ShouldBeFalse();
    }

    [Fact]
    public async Task AdminClient_ShouldHaveNoBrowserFlow()
    {
        var client = await app.Keycloak.Admin.GetClientAsync(CvitloKeycloak.AdminClientId, Ct);

        client["standardFlowEnabled"]!.GetValue<bool>().ShouldBeFalse();
        client["implicitFlowEnabled"]!.GetValue<bool>().ShouldBeFalse();
        client["directAccessGrantsEnabled"]!.GetValue<bool>().ShouldBeFalse();
        client["serviceAccountsEnabled"]!.GetValue<bool>().ShouldBeTrue();

        using var http = new HttpClient { BaseAddress = new Uri(app.Keycloak.BaseAddress) };
        using var response = await http.GetAsync(
            $"realms/{CvitloKeycloak.Realm}/protocol/openid-connect/auth?client_id={CvitloKeycloak.AdminClientId}"
            + "&response_type=code&redirect_uri=http%3A%2F%2Flocalhost%2Fcb", Ct);

        // Keycloak shows an error page (or redirects with an error) instead of its login form.
        var body = await response.Content.ReadAsStringAsync(Ct);
        body.ShouldNotContain("kc-form-login");
    }

    [Fact]
    public async Task AdminClientServiceAccount_ShouldHoldOnlyManageUsers()
    {
        var userId = await app.Keycloak.Admin.GetServiceAccountUserIdAsync(CvitloKeycloak.AdminClientId, Ct);

        var mappings = await app.Keycloak.Admin.GetRoleMappingsAsync(userId, Ct);

        var clientRoles = mappings["clientMappings"]!.AsObject()
            .SelectMany(client => client.Value!["mappings"]!.AsArray().Select(role => role!["name"]!.GetValue<string>()))
            .ToList();
        clientRoles.ShouldBe(["manage-users"]);
    }

    [Fact]
    public async Task AccountRestApi_WithTheUsersAccessToken_ShouldAnswer()
    {
        var user = app.Users[0];
        var tokens = await app.Keycloak.GetTokensAsync(user.Email, user.Password, Ct);
        using var http = new HttpClient { BaseAddress = new Uri(app.Keycloak.BaseAddress) };
        using var request = new HttpRequestMessage(HttpMethod.Get, $"realms/{CvitloKeycloak.Realm}/account");
        request.Headers.Authorization = new("Bearer", tokens["access_token"]!.GetValue<string>());
        request.Headers.Accept.Add(new("application/json"));

        using var response = await http.SendAsync(request, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PasswordGrant_ThroughAdminCli_ShouldBeRefused()
    {
        var user = app.Users[0];
        using var http = new HttpClient { BaseAddress = new Uri(app.Keycloak.BaseAddress) };

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["grant_type"] = "password",
                ["client_id"] = "admin-cli",
                ["username"] = user.Email,
                ["password"] = user.Password
            });
        using var response = await http.PostAsync($"realms/{CvitloKeycloak.Realm}/protocol/openid-connect/token", content, Ct);

        response.IsSuccessStatusCode.ShouldBeFalse();
    }

    [Fact]
    public async Task Google_ShouldRefuseAnEmailGoogleHasNotVerified()
    {
        var google = await app.Keycloak.Admin.GetIdentityProviderAsync("google", Ct);

        google["config"]!["filteredByClaim"]!.GetValue<string>().ShouldBe("true");
        google["config"]!["claimFilterName"]!.GetValue<string>().ShouldBe("email_verified");
        google["config"]!["claimFilterValue"]!.GetValue<string>().ShouldBe("true");
        google["firstBrokerLoginFlowAlias"]!.GetValue<string>().ShouldBe("cvitlo first broker login");
    }

    // Without its own prompt, Keycloak forwards the client's prompt to the provider. The register
    // endpoint sends prompt=create, which Google refuses with invalid_request.
    [Theory]
    [InlineData("google")]
    [InlineData("github")]
    public async Task SocialProvider_ShouldSendItsOwnPromptInsteadOfTheClients(string alias)
    {
        var provider = await app.Keycloak.Admin.GetIdentityProviderAsync(alias, Ct);

        ((string?)provider["config"]!["prompt"]).ShouldBe("select_account");
    }

    [Fact]
    public async Task FirstBrokerLogin_ShouldLinkOnlyAfterReauthentication()
    {
        var executions = await app.Keycloak.Admin.GetFlowExecutionsAsync("cvitlo first broker login", Ct);
        var byProvider = executions
            .Where(e => e!["providerId"] is not null)
            .ToDictionary(e => e!["providerId"]!.GetValue<string>(), e => e!["requirement"]!.GetValue<string>(), StringComparer.Ordinal);

        byProvider.ShouldNotContainKey("idp-auto-link");
        byProvider.ShouldNotContainKey("idp-email-verification");
        byProvider["idp-confirm-link"].ShouldBe("REQUIRED");
        byProvider["idp-username-password-form"].ShouldBe("REQUIRED");
    }

    [Fact]
    public async Task RepeatedWrongPasswords_ShouldLockTheAccountTemporarily()
    {
        var user = new TestUserFaker(777).Generate();
        await app.Keycloak.Admin.CreateUserAsync(user, ct: Ct);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await SubmitPasswordAsync(user.Email, "wrong-password-123");
        }

        using var response = await SubmitPasswordAsync(user.Email, user.Password);

        response.StatusCode.ShouldBe(HttpStatusCode.OK, "the right password is refused while the lockout lasts");
        response.Headers.Location.ShouldBeNull();
    }

    private async Task<HttpResponseMessage> SubmitPasswordAsync(string email, string password)
    {
        using var client = app.CreateBrowserClient();
        using var driver = new KeycloakLoginDriver(client);
        var page = await driver.StartAsync("api/v1/auth/login", Ct);

        return await driver.SubmitAsync(page, "kc-form-login", new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["username"] = email,
            ["password"] = password,
            ["credentialId"] = string.Empty
        }, Ct);
    }
}
