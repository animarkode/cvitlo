using ErrorOr;
using Cvitlo.Auth.Application.Sessions;
using FastEndpoints;

namespace Cvitlo.Auth.Application.Features.Account.EndOtherSessions;

public class EndOtherSessionsCommandHandler(KeycloakAccountClient keycloak)
    : ICommandHandler<EndOtherSessionsCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> ExecuteAsync(EndOtherSessionsCommand command, CancellationToken ct)
    {
        var deleted = await keycloak.DeleteOtherSessionsAsync(command.AccessToken, ct);
        if (deleted.IsError)
        {
            return deleted.FirstError;
        }

        return Result.Success;
    }
}
