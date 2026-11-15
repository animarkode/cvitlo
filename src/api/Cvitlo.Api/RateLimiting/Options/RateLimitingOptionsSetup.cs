using Cvitlo.Common;
using Microsoft.Extensions.Options;

namespace Cvitlo.Api.RateLimiting.Options;

public class RateLimitingOptionsSetup(IConfiguration configuration) : IConfigureOptions<RateLimitingOptions>
{
    public void Configure(RateLimitingOptions options)
    {
        configuration.GetSection(ConfigurationConstants.Sections.RateLimiting).Bind(options);
    }
}
