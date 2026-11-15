using Microsoft.AspNetCore.Hosting;

namespace Cvitlo.Resumes.Application.IntegrationTests;

// Redis points at a closed port and no Redis container starts (UsesRedis), so these tests prove
// the API keeps working, uncached, when Redis is unreachable.
public class RedisUnavailableWebApp : WebApp
{
    // A closed local port — connection attempts fail fast instead of resolving to a live server.
    private const string UnreachableRedisConnectionString = "127.0.0.1:1";

    protected override bool UsesRedis => false;

    protected override void ConfigureApp(IWebHostBuilder builder)
    {
        base.ConfigureApp(builder);

        builder.UseSetting("ConnectionStrings:redis", UnreachableRedisConnectionString);
    }
}
