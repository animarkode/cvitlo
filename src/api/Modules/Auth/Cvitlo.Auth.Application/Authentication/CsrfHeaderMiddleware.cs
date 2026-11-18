using Microsoft.AspNetCore.Http;

namespace Cvitlo.Auth.Application.Authentication;

// A forged cross-site request can carry the cookie but cannot add a custom header without a CORS preflight, which the API
// never grants. The cookie alone decides: an Authorization header never exempts a request that carries it. Bearer clients
// send no cookie, and the sign-in navigations and Keycloak's back-channel call are left alone.
public sealed class CsrfHeaderMiddleware(RequestDelegate next)
{
    private static readonly PathString Api = "/api";

    private static readonly PathString[] Navigations =
    [
        "/api/v1/auth/login",
        "/api/v1/auth/register",
        AuthSchemes.CallbackPath,
        "/api/v1/auth/backchannel-logout"
    ];

    private static readonly PathString AccountActionPrefix = "/api/v1/auth/account-action";

    public Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var needsHeader = request.Path.StartsWithSegments(Api, StringComparison.OrdinalIgnoreCase)
                          && request.Cookies.ContainsKey(AuthSchemes.SessionCookieName)
                          && !Navigations.Any(path => request.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                          && !IsAccountAction(request.Path);

        if (needsHeader && request.Headers[AuthSchemes.CsrfHeaderName] != "1")
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        return next(context);
    }

    private static bool IsAccountAction(PathString path)
    {
        return path.StartsWithSegments(AccountActionPrefix, StringComparison.OrdinalIgnoreCase, out var rest)
               && rest.HasValue
               && rest.Value.Length > 1
               && rest.Value.IndexOf('/', 1) < 0;
    }
}
