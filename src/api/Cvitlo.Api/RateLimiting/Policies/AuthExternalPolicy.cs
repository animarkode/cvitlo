using System.Threading.RateLimiting;
using Cvitlo.Api.RateLimiting.Options;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Cvitlo.Api.RateLimiting.Policies;

// A budget separate from the global anonymous baseline, so page traffic from a shared address
// cannot reject a callback returning from Keycloak and waste its single-use authorization code.
public sealed class AuthExternalPolicy(IOptions<RateLimitingOptions> options) : IRateLimiterPolicy<string>
{
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => null;

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        return RateLimitPartitions.SlidingWindow(RateLimitPartitions.ByIp(httpContext), options.Value.AuthExternal);
    }
}
