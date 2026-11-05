using Faber.Modules.Auth.Infrastructure.Database;
using Faber.Modules.Auth.PublicApi.Events;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Faber.Modules.Auth.Application.Sessions;

/// <summary>
/// Finishes account deletions that did not complete: a failed <see cref="UserDeleted"/> publish or a crash after
/// Keycloak deleted the user. A row is only touched once it is older than <see cref="MinAge"/>, so the sweep never
/// races a request that is still running.
/// </summary>
public sealed class PendingErasureSweeper(
    AuthDbContext db,
    KeycloakAdminClient keycloak,
    PostgresTicketStore sessions,
    TimeProvider time,
    ILogger<PendingErasureSweeper> logger)
{
    /// <summary>How old a pending erasure must be before the sweep takes it over.</summary>
    public static readonly TimeSpan MinAge = TimeSpan.FromMinutes(10);

    /// <summary>
    /// If Keycloak still knows the user the deletion never happened and the row is dropped. If the user is gone,
    /// sessions end, <see cref="UserDeleted"/> is published and the row is dropped. Any failure keeps the row for
    /// the next run.
    /// </summary>
    /// <returns>The number of rows resolved.</returns>
    public async Task<int> SweepAsync(CancellationToken ct)
    {
        var cutoff = time.GetUtcNow() - MinAge;
        var subjects = await db.PendingAccountErasures
            .Where(e => e.RequestedAt < cutoff)
            .Select(e => e.Subject)
            .ToListAsync(ct);

        var resolved = 0;
        foreach (var subject in subjects)
        {
            if (await ResolveAsync(subject, ct))
            {
                resolved++;
            }
        }

        return resolved;
    }

    private async Task<bool> ResolveAsync(string subject, CancellationToken ct)
    {
        var exists = await keycloak.UserExistsAsync(subject, ct);
        if (exists.IsError)
        {
            logger.LogWarning("Could not check whether {Subject} still exists ({Error}); the erasure is retried", subject, exists.FirstError.Code);
            return false;
        }

        if (!exists.Value)
        {
            try
            {
                await sessions.RemoveBySubjectAsync(subject);
                await new UserDeleted(subject).PublishAsync(Mode.WaitForAll, ct);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Erasing the data of {Subject} failed; the erasure is retried", subject);
                return false;
            }
        }

        await db.PendingAccountErasures.Where(e => e.Subject == subject).ExecuteDeleteAsync(ct);
        return true;
    }
}
