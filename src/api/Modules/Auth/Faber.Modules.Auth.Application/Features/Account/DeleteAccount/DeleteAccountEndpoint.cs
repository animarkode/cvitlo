using Faber.Modules.Auth.Application.Groups;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Faber.Modules.Auth.Application.Features.Account.DeleteAccount;

/// <summary>
/// Deletes the signed-in user's account: the Keycloak user, the user's data in every module and every web
/// session. Only a session that signed in within the last five minutes may do it.
/// </summary>
public class DeleteAccountEndpoint : EndpointWithoutRequest<Results<NoContent, ProblemHttpResult>>
{
    public override void Configure()
    {
        Delete("account");
        Group<AuthGroup>();
        Version(1);
    }

    public override async Task<Results<NoContent, ProblemHttpResult>> ExecuteAsync(CancellationToken ct)
    {
        // Session only: a bearer call has no stored tokens and is refused like the other account endpoints.
        var token = await HttpContext.GetAccessTokenAsync();
        if (token.IsError)
        {
            return token.FirstError.ToProblem();
        }

        var subject = User.GetSubject();
        if (subject.IsError)
        {
            return subject.FirstError.ToProblem();
        }

        var idToken = await HttpContext.GetTokenAsync(Authentication.AuthSchemes.Session, "id_token");

        var result = await DeleteAccountMapper.MapToCommand(subject.Value, idToken).ExecuteAsync(ct);
        if (result.IsError)
        {
            return result.FirstError.ToProblem();
        }

        await HttpContext.SignOutAsync(Authentication.AuthSchemes.Session);

        return TypedResults.NoContent();
    }
}
