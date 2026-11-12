using Cvitlo.Common;
using Microsoft.Extensions.Options;

namespace Cvitlo.Api.RateLimiting.Options;

/// <summary>Binds <see cref="RateLimitingOptions"/> from the <c>RateLimiting</c> configuration section.</summary>
public class RateLimitingOptionsSetup(IConfiguration configuration) : IConfigureOptions<RateLimitingOptions>
{
    public void Configure(RateLimitingOptions options)
    {
        configuration.GetSection(ConfigurationConstants.Sections.RateLimiting).Bind(options);
    }
}
