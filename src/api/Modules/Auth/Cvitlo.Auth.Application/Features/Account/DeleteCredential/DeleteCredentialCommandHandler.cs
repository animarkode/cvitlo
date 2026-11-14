using ErrorOr;
using Cvitlo.Auth.Application.Sessions;
using FastEndpoints;

namespace Cvitlo.Auth.Application.Features.Account.DeleteCredential;

/// <summary>Removes a stored credential, unless it is the user's last way to sign in.</summary>
public class DeleteCredentialCommandHandler(
    AccountSignInMethodsLock signInMethods,
    KeycloakAccountClient keycloak)
    : ICommandHandler<DeleteCredentialCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> ExecuteAsync(DeleteCredentialCommand command, CancellationToken ct)
    {
        var result = await signInMethods.RemoveAsync(
            command.Subject,
            command.AccessToken,
            (credentials, linked) => LastSignInMethodGuard.CheckCredentialRemoval(credentials, linked, command.Id),
            token => keycloak.DeleteCredentialAsync(command.AccessToken, command.Id, token),
            ct);

        if (result.IsError)
        {
            return result;
        }

        return Result.Success;
    }
}
