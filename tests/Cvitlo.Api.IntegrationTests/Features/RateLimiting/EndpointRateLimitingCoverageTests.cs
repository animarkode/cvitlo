using Cvitlo.Auth.Application.Features.Login;
using Cvitlo.Common.RateLimiting;
using FastEndpoints;
using FastEndpoints.Testing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Cvitlo.Api.IntegrationTests.Features.RateLimiting;

// Structural guards over every module's routes; like ResumesRateLimitingCoverageTests they read
// the built route table and spend no rate-limit budget.
[Collection<CollectionRateLimiting>]
[Priority(16)]
public class EndpointRateLimitingCoverageTests(WebApp app) : TestBase
{
    // A per-user policy on an anonymous endpoint lets a caller switch partitions by adding or
    // dropping a bearer token, which doubles the budget. Anonymous endpoints must count by IP.
    private static readonly string[] IpPartitionedPolicies = [RateLimitPolicies.AuthSession, RateLimitPolicies.AuthExternal];

    // Endpoints that deliberately carry no named policy and rely on the global limiter alone.
    // A new endpoint must either opt into a policy or be added here on purpose.
    private static readonly string[] BaselineOnlyEndpoints =
    [
        "DeleteAccountEndpoint",
        "DeleteCredentialEndpoint",
        "EndOtherSessionsEndpoint",
        "EndSessionEndpoint",
        "GetCredentialsEndpoint",
        "GetDevicesEndpoint",
        "GetLinkedAccountsEndpoint",
        "GetProfileEndpoint",
        "UnlinkAccountEndpoint",
        "UpdateProfileEndpoint",
        "UserEndpoint"
    ];

    [Fact]
    public void AnonymousEndpoint_ShouldUseAnIpPartitionedPolicy()
    {
        var anonymous = Routes().Where(route => route.AllowsAnonymous).ToList();

        // Guards the detection itself: an empty set would make the assertion below pass vacuously.
        anonymous.Select(route => route.EndpointType).ShouldContain(typeof(LoginEndpoint));

        var wrong = anonymous
            .Where(route => !IpPartitionedPolicies.Contains(route.PolicyName, StringComparer.Ordinal))
            .Select(route => $"{route.EndpointType.FullName} -> '{route.PolicyName ?? "(none)"}'")
            .Order(StringComparer.Ordinal)
            .ToList();

        wrong.ShouldBeEmpty(
            "These anonymous endpoints are not limited per client IP:"
            + $"{Environment.NewLine}{string.Join(Environment.NewLine, wrong)}");
    }

    [Fact]
    public void EndpointWithoutNamedPolicy_ShouldBeListedAsBaselineOnly()
    {
        var withoutPolicy = Routes()
            .Where(route => string.IsNullOrWhiteSpace(route.PolicyName))
            .Select(route => route.EndpointType.Name)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        withoutPolicy.ShouldBe(BaselineOnlyEndpoints);
    }

    private List<RoutedEndpoint> Routes()
    {
        return app.Services
            .GetRequiredService<EndpointDataSource>()
            .Endpoints
            .Select(endpoint => (Endpoint: endpoint, Definition: endpoint.Metadata.GetMetadata<EndpointDefinition>()))
            .Where(candidate => candidate.Definition is not null)
            .Select(candidate => new RoutedEndpoint(
                candidate.Definition!.EndpointType,
                candidate.Endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null,
                candidate.Endpoint.Metadata.GetMetadata<DisableRateLimitingAttribute>() is null
                    ? candidate.Endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName
                    : null))
            .ToList();
    }

    private sealed record RoutedEndpoint(Type EndpointType, bool AllowsAnonymous, string? PolicyName);
}
