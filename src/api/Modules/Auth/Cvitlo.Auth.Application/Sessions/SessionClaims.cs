using System.Security.Claims;
using System.Text.Json;
using Cvitlo.Auth.PublicApi;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Cvitlo.Auth.Application.Sessions;

// The cookie principal is built from the ID token, which carries no roles, so realm roles are copied from the
// access token at sign-in and on every refresh; bearer and cookie schemes then yield the same realm_access claim.
public static class SessionClaims
{
    // The token just came from Keycloak's token endpoint over an authenticated back channel, so it is read without re-validating.
    public static void ApplyAccessToken(ClaimsIdentity identity, string? accessToken)
    {
        foreach (var stale in identity.FindAll(JwtClaimTypes.Keycloak.RealmAccess).ToList())
        {
            identity.RemoveClaim(stale);
        }

        if (string.IsNullOrEmpty(accessToken))
        {
            return;
        }

        var token = new JsonWebToken(accessToken);
        if (token.TryGetPayloadValue<JsonElement>(JwtClaimTypes.Keycloak.RealmAccess, out var realmAccess))
        {
            identity.AddClaim(new Claim(
                JwtClaimTypes.Keycloak.RealmAccess,
                realmAccess.GetRawText(),
                JsonClaimValueTypes.Json));
        }
    }

    public static void ApplyIdToken(ClaimsIdentity identity, string? idToken)
    {
        if (string.IsNullOrEmpty(idToken))
        {
            return;
        }

        var token = new JsonWebToken(idToken);
        foreach (var type in ProfileClaims)
        {
            foreach (var stale in identity.FindAll(type).ToList())
            {
                identity.RemoveClaim(stale);
            }

            if (token.TryGetPayloadValue<string>(type, out var value) && !string.IsNullOrEmpty(value))
            {
                identity.AddClaim(new Claim(type, value));
            }
        }
    }

    private static readonly string[] ProfileClaims =
    [
        JwtClaimTypes.OidcStandard.Name,
        JwtClaimTypes.OidcStandard.GivenName,
        JwtClaimTypes.OidcStandard.FamilyName,
        JwtClaimTypes.OidcStandard.PreferredUsername,
        JwtClaimTypes.OidcStandard.Email
    ];

    public static string[] Roles(ClaimsPrincipal principal)
    {
        var realmAccess = principal.FindFirst(JwtClaimTypes.Keycloak.RealmAccess)?.Value;
        if (string.IsNullOrEmpty(realmAccess))
        {
            return [];
        }

        using var document = JsonDocument.Parse(realmAccess);
        return document.RootElement.TryGetProperty(JwtClaimTypes.KeycloakRealmAccess.Roles, out var roles)
            ? roles.EnumerateArray().Select(role => role.GetString()!).ToArray()
            : [];
    }
}
