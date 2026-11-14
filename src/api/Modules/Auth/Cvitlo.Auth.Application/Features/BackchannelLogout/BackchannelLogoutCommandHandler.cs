using ErrorOr;
using Cvitlo.Auth.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cvitlo.Auth.Application.Features.BackchannelLogout;

public class BackchannelLogoutCommandHandler(
    LogoutTokenValidator validator,
    AuthDbContext db,
    ILogger<BackchannelLogoutCommandHandler> logger)
    : ICommandHandler<BackchannelLogoutCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> ExecuteAsync(BackchannelLogoutCommand command, CancellationToken ct)
    {
        var target = await validator.ValidateAsync(command.LogoutToken, ct);
        if (target.IsError)
        {
            return target.Errors;
        }

        var sessions = string.IsNullOrEmpty(target.Value.KeycloakSessionId)
            ? db.Sessions.Where(session => session.Subject == target.Value.Subject)
            : db.Sessions.Where(session => session.KeycloakSessionId == target.Value.KeycloakSessionId);
        var ended = await sessions.ExecuteDeleteAsync(ct);

        logger.LogInformation("Back-channel logout ended {Count} web session(s)", ended);
        return Result.Success;
    }
}
