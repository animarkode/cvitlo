namespace Cvitlo.Api.RateLimiting.Options;

public class RateLimitingOptions
{
    // When false the rate limiting middleware is not added (integration test suites).
    public bool Enabled { get; set; } = true;

    public int RetryAfterFallbackSeconds { get; set; } = 60;

    public SlidingWindowPolicyOptions GlobalAnonymous { get; set; } = new() { PermitLimit = 60, WindowSeconds = 60 };

    public SlidingWindowPolicyOptions GlobalAuthenticated { get; set; } = new() { PermitLimit = 300, WindowSeconds = 60 };

    public SlidingWindowPolicyOptions AuthSession { get; set; } = new() { PermitLimit = 20, WindowSeconds = 60 };

    public SlidingWindowPolicyOptions AuthExternal { get; set; } = new() { PermitLimit = 20, WindowSeconds = 60 };

    public SlidingWindowPolicyOptions AuthenticatedDefault { get; set; } = new() { PermitLimit = 100, WindowSeconds = 60 };

    public ConcurrencyPolicyOptions ExpensiveResource { get; set; } = new() { PermitLimit = 2, QueueLimit = 2 };

}
