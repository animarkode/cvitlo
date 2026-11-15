using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;

namespace Cvitlo.Api.Caching;

// Degradation posture, TTLs and keys: docs/decisions/caching.md.
public static class CachingServiceCollectionExtensions
{
    private const long MaximumPayloadBytes = 10 * 1024 * 1024;

    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(10);

    private static readonly TimeSpan DefaultLocalCacheExpiration = TimeSpan.FromMinutes(1);

    // Short timeouts so a Redis outage fails fast instead of queuing behind the client's 5 second backlog.
    private static readonly TimeSpan RedisTimeout = TimeSpan.FromMilliseconds(1000);

    public static IServiceCollection AddCvitloCaching(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHybridCache(o =>
        {
            o.MaximumPayloadBytes = MaximumPayloadBytes;
            o.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = DefaultExpiration,
                LocalCacheExpiration = DefaultLocalCacheExpiration
            };
        });

        var redisConnectionString = configuration.GetConnectionString("redis");

        if (string.IsNullOrEmpty(redisConnectionString))
        {
            return services;
        }

        var configurationOptions = ConfigurationOptions.Parse(redisConnectionString);
        configurationOptions.AbortOnConnectFail = false;
        configurationOptions.BacklogPolicy = BacklogPolicy.FailFast;
        configurationOptions.ConnectTimeout = (int)RedisTimeout.TotalMilliseconds;
        configurationOptions.SyncTimeout = (int)RedisTimeout.TotalMilliseconds;
        configurationOptions.AsyncTimeout = (int)RedisTimeout.TotalMilliseconds;

        // One multiplexer for the cache and for OpenTelemetry's Redis instrumentation, which
        // resolves IConnectionMultiplexer from DI. Connected lazily and once; AbortOnConnectFail=false
        // means connecting never throws while Redis is down.
        var sharedConnection = new Lazy<Task<IConnectionMultiplexer>>(
            async () => await ConnectionMultiplexer.ConnectAsync(configurationOptions));

        services.AddSingleton<IConnectionMultiplexer>(_ => sharedConnection.Value.GetAwaiter().GetResult());

        services.AddStackExchangeRedisCache(o =>
        {
            o.InstanceName = "cvitlo:";

            // The factory wins over this, but it keeps the fail-fast settings inspectable.
            o.ConfigurationOptions = configurationOptions;
            o.ConnectionMultiplexerFactory = () => sharedConnection.Value;
        });

        return services;
    }
}
