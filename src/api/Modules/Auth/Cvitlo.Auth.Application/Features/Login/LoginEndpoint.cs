using Cvitlo.Auth.Application.Authentication;
using Cvitlo.Auth.Application.Groups;
using Cvitlo.Common.RateLimiting;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Cvitlo.Auth.Application.Features.Login;

/// <summary>Sends the browser to Keycloak's sign-in page; the browser navigates here, it does not fetch.</summary>
public class LoginEndpoint : EndpointWithoutRequest<Results<ChallengeHttpResult, RedirectHttpResult>>
{
    public override void Configure()
    {
        Get("login");
        Group<AuthGroup>();
        Version(1);
        AllowAnonymous();
        Options(x => x.RequireRateLimiting(RateLimitPolicies.AuthExternal));
    }

    public override Task<Results<ChallengeHttpResult, RedirectHttpResult>> ExecuteAsync(CancellationToken ct)
    {
        return Task.FromResult(KeycloakChallenge.For(User, Query<string>("returnUrl", isRequired: false), prompt: null));
    }
}
