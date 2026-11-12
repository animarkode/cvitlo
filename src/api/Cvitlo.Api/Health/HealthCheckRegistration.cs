using Cvitlo.Auth.Infrastructure.Database;
using Cvitlo.Resumes.Infrastructure.Database;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cvitlo.Api.Health;

public static class HealthCheckRegistration
{
    public static IServiceCollection AddApiReadinessChecks(this IServiceCollection services)
    {
        services.AddHttpClient("health-keycloak", client => client.Timeout = TimeSpan.FromSeconds(4));

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck<AuthDbContext>>(
                "auth-db", failureStatus: HealthStatus.Unhealthy, tags: ["ready"],
                timeout: TimeSpan.FromSeconds(5))
            .AddCheck<DatabaseHealthCheck<ResumesDbContext>>(
                "resumes-db", failureStatus: HealthStatus.Unhealthy, tags: ["ready"],
                timeout: TimeSpan.FromSeconds(5))
            .AddCheck<KeycloakHealthCheck>(
                "keycloak", failureStatus: HealthStatus.Unhealthy, tags: ["ready"],
                timeout: TimeSpan.FromSeconds(5));

        return services;
    }
}
