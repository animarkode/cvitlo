using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Cvitlo.Common.RateLimiting;

/// <summary>
/// The one producer of the throttled-request response, shared by the Api middleware and by endpoints
/// that throttle on request-body data it cannot see. Both paths must be indistinguishable to a caller:
/// same status, <c>Retry-After</c> and body, with no partition keys, addresses or limit values.
/// </summary>
public static class RateLimitRejection
{
    /// <summary>ProblemDetails title returned on every rate limit rejection.</summary>
    public const string Title = "Too Many Requests";

    /// <summary>ProblemDetails detail — deliberately generic, it must leak nothing about which limiter rejected.</summary>
    public const string Detail = "Rate limit exceeded. Please retry later.";

    /// <summary>Sets <c>Retry-After</c> on <paramref name="context"/> and builds the 429 problem result with the trace id.</summary>
    public static ProblemHttpResult Problem(HttpContext context, int retryAfterSeconds)
    {
        context.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

        return TypedResults.Problem(
            statusCode: StatusCodes.Status429TooManyRequests,
            title: Title,
            detail: Detail,
            extensions: new Dictionary<string, object?>(StringComparer.Ordinal) { ["traceId"] = context.TraceIdentifier });
    }
}
