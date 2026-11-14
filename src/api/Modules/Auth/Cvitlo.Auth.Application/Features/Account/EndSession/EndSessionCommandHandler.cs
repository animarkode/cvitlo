using ErrorOr;
using Cvitlo.Auth.Application.Sessions;
using FastEndpoints;

namespace Cvitlo.Auth.Application.Features.Account.EndSession;

/// <summary>
/// Ends one other session of the user. The current session is refused: the user signs out instead. Keycloak's
/// back-channel logout then removes the ended session's app session.
/// </summary>
public class EndSessionCommandHandler(KeycloakAccountClient keycloak)
    : ICommandHandler<EndSessionCommand, ErrorOr<Success>>
{
    public const string CurrentSessionCode = "Account.CurrentSession";
    public const string SessionNotFoundCode = "Account.SessionNotFound";

    public async Task<ErrorOr<Success>> ExecuteAsync(EndSessionCommand command, CancellationToken ct)
    {
        var devices = await keycloak.GetDevicesAsync(command.AccessToken, ct);
        if (devices.IsError)
        {
            return devices.FirstError;
        }

        var session = devices.Value
            .SelectMany(device => device.Sessions ?? [])
            .FirstOrDefault(s => string.Equals(s.Id, command.Id, StringComparison.Ordinal));

        if (session is null)
        {
            return Error.NotFound(SessionNotFoundCode, "The session was not found.");
        }

        if (session.Current)
        {
            return Error.Conflict(CurrentSessionCode, "This is the current session; sign out instead.");
        }

        var deleted = await keycloak.DeleteSessionAsync(command.AccessToken, command.Id, ct);
        if (deleted.IsError)
        {
            return deleted.FirstError;
        }

        return Result.Success;
    }
}
