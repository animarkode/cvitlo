using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Cvitlo.Auth.Application.Authentication;

// A request is one identity: endpoints that read the session directly would otherwise mix it with a bearer principal, so
// any /api request with the cookie and a bearer header is refused. A forged cross-site request can carry the cookie but not
// a custom header without a CORS preflight, which the API never grants; only top-level navigations go without it.
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
        var carriesSession = request.Path.StartsWithSegments(Api, StringComparison.OrdinalIgnoreCase)
                             && request.Cookies.ContainsKey(AuthSchemes.SessionCookieName);

        if (!carriesSession)
        {
            return next(context);
        }

        if (AuthSchemes.IsBearerRequest(request))
        {
            return Refuse(context, StatusCodes.Status400BadRequest, "Auth.MixedCredentials",
                "Send either the session cookie or a bearer token, not both.");
        }

        var isNavigation = Navigations.Any(path => request.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                           || IsAccountAction(request.Path);

        if (!isNavigation && request.Headers[AuthSchemes.CsrfHeaderName] != "1")
        {
            return Refuse(context, StatusCodes.Status403Forbidden, "Auth.CsrfHeaderMissing",
                $"Send the {AuthSchemes.CsrfHeaderName}: 1 header with requests that carry the session cookie.");
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
