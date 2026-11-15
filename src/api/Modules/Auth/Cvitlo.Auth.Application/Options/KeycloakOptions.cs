namespace Cvitlo.Auth.Application.Options;

public class KeycloakOptions
{
    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    // Service account limited to manage-users; used only to delete a user's own account.
    public string AdminClientId { get; set; } = "cvitlo-admin";

    public string AdminClientSecret { get; set; } = string.Empty;

    // Off only for a local Keycloak served over plain HTTP.
    public bool RequireHttpsMetadata { get; set; } = true;

    // The realm's SSO idle timeout without "Remember me": a refresh token that outlives it marks a
    // session started with "Remember me", which gets a persistent cookie.
    public int DefaultSessionIdleSeconds { get; set; } = 1800;

    public string TokenEndpoint => $"{Issuer}/protocol/openid-connect/token";

    public string LogoutEndpoint => $"{Issuer}/protocol/openid-connect/logout";

    public string AccountApiUrl => $"{Issuer}/account";

    public string AdminUsersUrl => Issuer.Replace("/realms/", "/admin/realms/", StringComparison.Ordinal) + "/users";
}
