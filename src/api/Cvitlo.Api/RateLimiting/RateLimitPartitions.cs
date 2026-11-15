using System.Security.Claims;
using System.Threading.RateLimiting;
using Cvitlo.Api.RateLimiting.Options;

namespace Cvitlo.Api.RateLimiting;

// Keys are prefixed (ip: / user:) so an IP can never collide with a user id.
public static class RateLimitPartitions
{
    private const string SubClaim = "sub";

    // Reads the address after ForwardedHeaders has applied the trusted proxy.
    public static string ByIp(HttpContext context)
    {
        return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }

    public static string? ByUser(HttpContext context)
    {
        return context.User.FindFirstValue(SubClaim) is { Length: > 0 } sub ? $"user:{sub}" : null;
    }

    public static string ByUserOrIp(HttpContext context)
    {
        return ByUser(context) ?? ByIp(context);
    }

    public static RateLimitPartition<string> SlidingWindow(string key, SlidingWindowPolicyOptions policy)
    {
        return RateLimitPartition.GetSlidingWindowLimiter(
            key,
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = policy.PermitLimit,
                Window = TimeSpan.FromSeconds(policy.WindowSeconds),
                SegmentsPerWindow = policy.SegmentsPerWindow,
                QueueLimit = 0,
                AutoReplenishment = true
            });
    }

    public static RateLimitPartition<string> Concurrency(string key, ConcurrencyPolicyOptions policy)
    {
        return RateLimitPartition.GetConcurrencyLimiter(
            key,
            _ => new ConcurrencyLimiterOptions
            {
                PermitLimit = policy.PermitLimit,
                QueueLimit = policy.QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    }
}
