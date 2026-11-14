using Cvitlo.Auth.Application.Groups;
using Cvitlo.Common.RateLimiting;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Cvitlo.Auth.Application.Features.Logout;

/// <summary>
/// Signs the browser out: ends the Keycloak session, then deletes the server-side session and the cookie.
/// The web session is deleted even when Keycloak cannot be reached, so the cookie is dead either way.
/// </summary>
public class LogoutEndpoint : EndpointWithoutRequest<NoContent>
{
    public override void Configure()
    {
        Post("logout");
        Group<AuthGroup>();
        Version(1);
        AllowAnonymous();
        Options(x => x.RequireRateLimiting(RateLimitPolicies.AuthSession));
    }

    public override async Task<NoContent> ExecuteAsync(CancellationToken ct)
    {
        var session = await HttpContext.AuthenticateAsync(Authentication.AuthSchemes.Session);

        if (session.Succeeded)
        {
            await new LogoutCommand(session.Properties.GetTokenValue("refresh_token")).ExecuteAsync(ct);
            await HttpContext.SignOutAsync(Authentication.AuthSchemes.Session);
        }

        return TypedResults.NoContent();
    }
}
