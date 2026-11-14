using System.Threading.RateLimiting;
using Cvitlo.Api.RateLimiting.Options;
using Cvitlo.Common.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Cvitlo.Api.RateLimiting;

/// <summary>
/// Handles every rejected request: derives <c>Retry-After</c> from the failing lease (falling back to
/// <see cref="RateLimitingOptions.RetryAfterFallbackSeconds"/>), records the rejection metric, logs a
/// structured warning, and writes the shared <see cref="RateLimitRejection"/> response.
/// </summary>
public static class RateLimitRejectionHandler
{
    /// <summary>Metric/log tag used when only the global baseline limiter applies.</summary>
    private const string GlobalPolicyName = "global";

    private const string LogCategory = "Cvitlo.Api.RateLimiting";

    /// <summary>Wired as <c>RateLimiterOptions.OnRejected</c>; <paramref name="ct"/> is required by that delegate.</summary>
    /// <param name="context">The rejection context supplied by the ASP.NET Core rate limiter.</param>
    /// <param name="ct">The cancellation token supplied by the framework delegate signature; unused.</param>
    /// <returns>A task that completes once the rejection response has been written.</returns>
    public static ValueTask HandleAsync(OnRejectedContext context, CancellationToken ct)
    {
        return HandleAsync(context, policyName: null, ct);
    }

    /// <summary>Handles a rejection from a limiter that runs outside endpoint routing, where no endpoint names the policy.</summary>
    /// <param name="context">The rejection context supplied by the ASP.NET Core rate limiter.</param>
    /// <param name="policyName">The policy to record, or <see langword="null"/> to read it from the endpoint.</param>
    /// <param name="ct">The cancellation token supplied by the framework delegate signature; unused.</param>
    /// <returns>A task that completes once the rejection response has been written.</returns>
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
