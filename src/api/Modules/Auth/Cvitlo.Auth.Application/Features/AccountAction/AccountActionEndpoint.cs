using Cvitlo.Auth.Application.Authentication;
using Cvitlo.Auth.Application.Groups;
using Cvitlo.Auth.Application.Sessions;
using Cvitlo.Auth.PublicApi;
using Cvitlo.Common.RateLimiting;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Cvitlo.Auth.Application.Features.AccountAction;

/// <summary>
/// Starts a Keycloak Application Initiated Action (change password, add a passkey, delete the account, ...)
/// for the signed-in user; the browser navigates here, it does not fetch. The route value picks an entry of
/// <see cref="AccountActions"/>; nothing else reaches Keycloak. Keycloak performs the action on its own page
/// and the callback brings the browser back to the account page with the outcome.
/// </summary>
public class AccountActionEndpoint : EndpointWithoutRequest<Results<ChallengeHttpResult, NotFound>>
{
    public override void Configure()
    {
        Get("account-action/{action}");
        Group<AuthGroup>();
        Version(1);
        Options(x => x.RequireRateLimiting(RateLimitPolicies.AuthExternal));
    }

    public override async Task<Results<ChallengeHttpResult, NotFound>> ExecuteAsync(CancellationToken ct)
    {
        var action = Route<string>("action", isRequired: false);
        if (!AccountActions.IsAllowed(action))
        {
            return TypedResults.NotFound();
        }

        var properties = new AuthenticationProperties { RedirectUri = AccountActions.ReturnPath(action!, AccountActions.StatusError) };
        if (AccountActions.TryGetKcAction(action, out var kcAction))
        {
            properties.Items[OidcParameters.KcAction] = kcAction;
        }
        else
        {
            properties.Items[OidcParameters.Prompt] = "login";
        }

        properties.Items[OidcParameters.AccountAction] = action;

        // The callback runs without the session cookie (SameSite=Strict, arriving from Keycloak), so the
        // session to replace travels in the signed, encrypted OIDC state. Only its hash, never the cookie key.
        var session = await HttpContext.AuthenticateAsync(Authentication.AuthSchemes.Session);
        if (session.Properties is not null
            && session.Properties.Items.TryGetValue(PostgresTicketStore.SessionKeyItem, out var key)
            && key is not null)
        {
            properties.Items[OidcParameters.ReplacesSession] = PostgresTicketStore.HashKey(key);
        }

        // The callback may only complete for the user who started the action.
        var subject = session.Principal?.FindFirst(JwtClaimTypes.Registered.Sub)?.Value;
        if (!string.IsNullOrEmpty(subject))
        {
            properties.Items[OidcParameters.AccountSubject] = subject;
        }

        var idToken = await HttpContext.GetTokenAsync(Authentication.AuthSchemes.Session, "id_token");
        if (!string.IsNullOrEmpty(idToken))
        {
            properties.Items[OidcParameters.IdTokenHint] = idToken;
        }

        return TypedResults.Challenge(properties, [Authentication.AuthSchemes.Keycloak]);
    }
}
