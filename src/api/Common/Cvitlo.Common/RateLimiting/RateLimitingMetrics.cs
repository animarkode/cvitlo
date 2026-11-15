using System.Diagnostics.Metrics;

namespace Cvitlo.Common.RateLimiting;

/// <summary>
/// The single rejection counter, shared by the Api middleware and module-level limiters below HTTP.
/// It lives in Common because a module may not reference the API host, and one meter keeps
/// "how much are we throttling, and where?" a single dashboard query.
/// </summary>
public static class RateLimitingMetrics
{
    /// <summary>Must match the meter registered in Cvitlo.ServiceDefaults.</summary>
    public const string MeterName = "Cvitlo.Api.RateLimiting";

    /// <summary>Exposed so tests can subscribe with a <see cref="System.Diagnostics.Metrics.MeterListener"/> without duplicating the string.</summary>
    public const string RejectionsCounterName = "cvitlo.api.rate_limiting.rejections";

    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> Rejections = Meter.CreateCounter<long>(
        RejectionsCounterName,
        description: "Number of requests rejected by the rate limiter.");

    /// <summary>Records a single rejection for the given policy or limiter name ("global" for the global limiter).</summary>
    public static void RecordRejection(string policy)
    {
        Rejections.Add(1, new KeyValuePair<string, object?>("policy", policy));
    }
}
