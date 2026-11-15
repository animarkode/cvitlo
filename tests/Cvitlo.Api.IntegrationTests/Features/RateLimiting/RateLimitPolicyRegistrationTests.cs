using System.Collections;
using System.Reflection;
using Cvitlo.Common.RateLimiting;
using FastEndpoints.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Cvitlo.Api.IntegrationTests.Features.RateLimiting;

// Proves every declared RateLimitPolicies constant is registered by AddCvitloRateLimiting; otherwise a missing
// AddPolicy call compiles clean and the gap shows up at request time in production.
[Collection<CollectionRateLimiting>]
public class RateLimitPolicyRegistrationTests(WebApp app) : TestBase
{
    private static readonly HashSet<string> DeclaredPolicyNames = typeof(RateLimitPolicies)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field is { IsLiteral: true, IsInitOnly: false } && field.FieldType == typeof(string))
        .Select(field => (string)field.GetRawConstantValue()!)
        .ToHashSet(StringComparer.Ordinal);

    [Fact]
    public void EveryRateLimitPolicyConstant_ShouldBeRegisteredByAddCvitloRateLimiting()
    {
        DeclaredPolicyNames.ShouldNotBeEmpty();

        var unregistered = DeclaredPolicyNames
            .Except(RegisteredPolicyNames(), StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        unregistered.ShouldBeEmpty(
            "These RateLimitPolicies constants have no matching limiter.AddPolicy<string, TPolicy>(...) call "
            + "in AddCvitloRateLimiting, so an endpoint could opt in via RequireRateLimiting to a name the "
            + $"limiter has never heard of and fail only at request time:{Environment.NewLine}"
            + string.Join(Environment.NewLine, unregistered));
    }

    // RateLimiterOptions exposes no way to list policy names: AddPolicy stores factories in internal maps,
    // the second filled only once a request resolves the policy. Reflection avoids one request per policy,
    // which the shared budget fixture forbids.
    private HashSet<string> RegisteredPolicyNames()
    {
        var options = app.Services.GetRequiredService<IOptions<RateLimiterOptions>>().Value;
        var optionsType = typeof(RateLimiterOptions);

        var policyMap = (IDictionary)optionsType
            .GetProperty("PolicyMap", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(options)!;

        var unactivatedPolicyMap = (IDictionary)optionsType
            .GetProperty("UnactivatedPolicyMap", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(options)!;

        return policyMap.Keys.Cast<string>()
            .Concat(unactivatedPolicyMap.Keys.Cast<string>())
            .ToHashSet(StringComparer.Ordinal);
    }
}
