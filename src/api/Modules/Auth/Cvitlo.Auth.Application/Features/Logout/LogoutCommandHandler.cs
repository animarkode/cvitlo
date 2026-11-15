using ErrorOr;
using Cvitlo.Auth.Application.Sessions;
using FastEndpoints;

namespace Cvitlo.Auth.Application.Features.Logout;

// Back channel with the refresh token and client secret, so the browser needs no ID token or logout URL.
public class LogoutCommandHandler(KeycloakTokenClient keycloak)
    : ICommandHandler<LogoutCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> ExecuteAsync(LogoutCommand command, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(command.RefreshToken))
        {
            return Result.Success;
        }

        if (!await keycloak.LogoutAsync(command.RefreshToken, ct))
        {
            return Error.Failure("Auth.Logout", "Keycloak did not end its session.");
        }

        return Result.Success;
    }
}
