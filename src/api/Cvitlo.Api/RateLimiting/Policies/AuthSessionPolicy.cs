using System.Threading.RateLimiting;
using Cvitlo.Api.RateLimiting.Options;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Cvitlo.Api.RateLimiting.Policies;

// Back-channel logout is anonymous by design (Keycloak posts a signed logout token), so keying on a
// user would be meaningless; partitioning by IP bounds how fast anyone can probe it with forged tokens.
public sealed class AuthSessionPolicy(IOptions<RateLimitingOptions> options) : IRateLimiterPolicy<string>
{
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => null;

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        return RateLimitPartitions.SlidingWindow(RateLimitPartitions.ByIp(httpContext), options.Value.AuthSession);
    }
}
