namespace Faber.Modules.Auth.Application.Options;

/// <summary>
/// How the API reaches Keycloak as the web app's confidential client. All values are bound from the
/// <c>Keycloak</c> configuration section; <see cref="ClientId"/> and <see cref="ClientSecret"/> are required.
/// </summary>
public class KeycloakOptions
{
    /// <summary>The public issuer, e.g. <c>https://keycloak.example.com/realms/faber</c>.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>The audience every access token must carry.</summary>
    public string Audience { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// The confidential client whose service account may only manage users (the Admin API). Used solely to
    /// delete a user's own account; it has no sign-in flow.
    /// </summary>
    public string AdminClientId { get; set; } = "faber-admin";

    /// <summary>The secret of <see cref="AdminClientId"/>.</summary>
    public string AdminClientSecret { get; set; } = string.Empty;

    /// <summary>Off only for a local Keycloak served over plain HTTP.</summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// The realm's SSO idle timeout without "Remember me". A session whose refresh token outlives it was
    /// started with "Remember me" ticked, and gets a persistent cookie.
    /// </summary>
    public int DefaultSessionIdleSeconds { get; set; } = 1800;

    public string TokenEndpoint => $"{Issuer}/protocol/openid-connect/token";

    public string LogoutEndpoint => $"{Issuer}/protocol/openid-connect/logout";

    /// <summary>Keycloak's Account REST API, called with the signed-in user's own access token.</summary>
    public string AccountApiUrl => $"{Issuer}/account";

    /// <summary>
    /// Keycloak's Admin API users collection, derived from <see cref="Issuer"/>
    /// (<c>{server}/realms/{realm}</c> becomes <c>{server}/admin/realms/{realm}/users</c>).
    /// </summary>
    public string AdminUsersUrl => Issuer.Replace("/realms/", "/admin/realms/", StringComparison.Ordinal) + "/users";
}
