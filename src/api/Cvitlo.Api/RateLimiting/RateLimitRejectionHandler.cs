using System.Threading.RateLimiting;
using Cvitlo.Api.RateLimiting.Options;
using Cvitlo.Common.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Cvitlo.Api.RateLimiting;

public static class RateLimitRejectionHandler
{
    private const string GlobalPolicyName = "global";

    private const string LogCategory = "Cvitlo.Api.RateLimiting";

    public static ValueTask HandleAsync(OnRejectedContext context, CancellationToken ct)
    {
        return HandleAsync(context, policyName: null, ct);
    }

    public static async ValueTask HandleAsync(OnRejectedContext context, string? policyName, CancellationToken ct)
    {
        var httpContext = context.HttpContext;
        var options = httpContext.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var metadata)
            ? metadata
            : TimeSpan.FromSeconds(options.RetryAfterFallbackSeconds);

        var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));

        var policy = policyName
                     ?? httpContext.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName
                     ?? GlobalPolicyName;

        RateLimitingMetrics.RecordRejection(policy);

        var logger = httpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(LogCategory);

        // The route pattern comes from the endpoint table, not from the request, so a caller cannot
        // inject line breaks or forged entries into the log through the requested path.
        var route = httpContext.GetEndpoint() is RouteEndpoint routeEndpoint
            ? routeEndpoint.RoutePattern.RawText
            : null;

        logger.LogWarning(
            "Rate limit exceeded: policy {Policy}, route {Route}, retry after {RetryAfterSeconds}s",
            policy,
            route ?? "(unmatched)",
            retryAfterSeconds);

        await RateLimitRejection.Problem(httpContext, retryAfterSeconds).ExecuteAsync(httpContext);
    }
}
