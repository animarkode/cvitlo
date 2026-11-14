using ErrorOr;
using Cvitlo.Auth.Application.Sessions;
using FastEndpoints;

namespace Cvitlo.Auth.Application.Features.Account.UnlinkAccount;

/// <summary>Unlinks a provider, unless it is the user's last way to sign in.</summary>
public class UnlinkAccountCommandHandler(
    AccountSignInMethodsLock signInMethods,
    KeycloakAccountClient keycloak)
    : ICommandHandler<UnlinkAccountCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> ExecuteAsync(UnlinkAccountCommand command, CancellationToken ct)
    {
        var result = await signInMethods.RemoveAsync(
            command.Subject,
            command.AccessToken,
            (credentials, linked) => LastSignInMethodGuard.CheckUnlink(credentials, linked, command.Alias),
            token => keycloak.DeleteLinkedAccountAsync(command.AccessToken, command.Alias, token),
            ct);

        if (result.IsError)
        {
            return result;
        }

        return Result.Success;
    }
}
