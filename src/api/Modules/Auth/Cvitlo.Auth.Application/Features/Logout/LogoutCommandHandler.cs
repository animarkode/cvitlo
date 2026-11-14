using ErrorOr;
using Cvitlo.Auth.Application.Sessions;
using FastEndpoints;

namespace Cvitlo.Auth.Application.Features.Logout;

/// <summary>
/// Ends the Keycloak session on the back channel, with the session's refresh token and the client secret,
/// so the browser never needs an ID token or a logout URL.
/// </summary>
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
