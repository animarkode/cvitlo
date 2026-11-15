namespace Cvitlo.Api.RateLimiting.Options;

public class ConcurrencyPolicyOptions
{
    public int PermitLimit { get; set; }

    public int QueueLimit { get; set; }
}
