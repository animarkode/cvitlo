using Microsoft.AspNetCore.Hosting;

namespace Cvitlo.Resumes.Application.IntegrationTests;

// Points ConnectionStrings:redis at a closed port instead of the Testcontainers Redis
// instance, so tests in this fixture prove the API keeps working (uncached) when Redis is
// unreachable, per the graceful-degradation requirement. The Redis container from
// WebApp is never built or started for this fixture — see UsesRedis.
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
