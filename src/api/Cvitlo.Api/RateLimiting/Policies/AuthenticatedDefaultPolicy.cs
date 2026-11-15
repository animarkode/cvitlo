using System.Threading.RateLimiting;
using Cvitlo.Api.RateLimiting.Options;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Cvitlo.Api.RateLimiting.Policies;

public sealed class AuthenticatedDefaultPolicy(IOptions<RateLimitingOptions> options) : IRateLimiterPolicy<string>
{
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => null;

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        return RateLimitPartitions.SlidingWindow(RateLimitPartitions.ByUserOrIp(httpContext), options.Value.AuthenticatedDefault);
    }
}
