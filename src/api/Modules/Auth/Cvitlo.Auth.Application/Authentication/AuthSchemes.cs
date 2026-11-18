using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Cvitlo.Auth.Application.Authentication;

public static class AuthSchemes
{
    public const string Default = "cvitlo";

    public const string Session = "cvitlo-session";

    public const string Keycloak = "keycloak";

    public const string Bearer = JwtBearerDefaults.AuthenticationScheme;

    // __Host- makes the browser refuse the cookie unless it is Secure, has Path=/ and no Domain, so no subdomain can set or read it.
    public const string SessionCookieName = "__Host-cvitlo";

    // A cross-site page cannot add a custom header without a CORS preflight, which the API never grants.
    public const string CsrfHeaderName = "X-CSRF";

    public const string CallbackPath = "/api/v1/auth/callback";

    // Shared by the scheme selector and the mixed-credentials guard, so no request is bearer to one and cookie to the other.
    public static bool IsBearerRequest(Microsoft.AspNetCore.Http.HttpRequest request)
    {
        return request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
    }
}
