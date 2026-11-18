using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Cvitlo.Auth.Application.Authentication;

// A forged cross-site request can carry the cookie but cannot add a custom header without a CORS preflight, which the API
// never grants. A request is one identity: endpoints that read the session directly would otherwise mix it with a bearer
// principal, so a cookie request that also sends a bearer header is refused.
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

        if (!needsHeader)
        {
            return next(context);
        }

        if (request.Headers[AuthSchemes.CsrfHeaderName] != "1")
        {
            return Refuse(context, StatusCodes.Status403Forbidden, "Auth.CsrfHeaderMissing",
                $"Send the {AuthSchemes.CsrfHeaderName}: 1 header with requests that carry the session cookie.");
        }

        if (AuthSchemes.IsBearerRequest(request))
        {
            return Refuse(context, StatusCodes.Status400BadRequest, "Auth.MixedCredentials",
                "Send either the session cookie or a bearer token, not both.");
        }

        return next(context);
    }

    private static Task Refuse(HttpContext context, int statusCode, string code, string detail)
    {
        return TypedResults.Problem(
                detail,
                statusCode: statusCode,
                extensions: new Dictionary<string, object?>(StringComparer.Ordinal) { ["code"] = code })
            .ExecuteAsync(context);
    }

    private static bool IsAccountAction(PathString path)
    {
        return path.StartsWithSegments(AccountActionPrefix, StringComparison.OrdinalIgnoreCase, out var rest)
               && rest.HasValue
               && rest.Value.Length > 1
               && rest.Value.IndexOf('/', 1) < 0;
    }
}
