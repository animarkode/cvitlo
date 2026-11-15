namespace Cvitlo.Api.RateLimiting.Options;

public class SlidingWindowPolicyOptions
{
    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; }

    public int SegmentsPerWindow { get; set; } = 6;
}
