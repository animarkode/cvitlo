using Cvitlo.Api.Health;
using Cvitlo.Common.Testing.Containers;
using FastEndpoints.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;

namespace Cvitlo.Api.IntegrationTests.Features.Health;

[Collection<CollectionRateLimiting>]
[Priority(21)]
public class KeycloakHealthCheckTests(WebApp app) : TestBase
{
    [Fact]
    public async Task CheckHealth_RealmReachable_ShouldReportHealthy()
    {
        var result = await CheckAsync(app.Keycloak.BaseAddress, CvitloKeycloak.Realm);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealth_RealmUnknownToKeycloak_ShouldReportUnhealthy()
    {
        var result = await CheckAsync(app.Keycloak.BaseAddress, "no-such-realm");

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealth_KeycloakUnreachable_ShouldReportUnhealthy()
    {
        var result = await CheckAsync("http://127.0.0.1:1/", CvitloKeycloak.Realm);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Theory]
    [InlineData(null, "cvitlo")]
    [InlineData("not-a-url", "cvitlo")]
    [InlineData("http://127.0.0.1:1/", null)]
    public async Task CheckHealth_KeycloakNotConfigured_ShouldReportUnhealthy(string? baseUrl, string? realm)
    {
        var result = await CheckAsync(baseUrl, realm);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    private static async Task<HealthCheckResult> CheckAsync(string? baseUrl, string? realm)
    {
        await using var services = new ServiceCollection().AddHttpClient().BuildServiceProvider();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["Keycloak:BaseUrl"] = baseUrl,
                ["Keycloak:Realm"] = realm
            })
            .Build();
        var check = new KeycloakHealthCheck(services.GetRequiredService<IHttpClientFactory>(), configuration);

        return await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);
    }
}
