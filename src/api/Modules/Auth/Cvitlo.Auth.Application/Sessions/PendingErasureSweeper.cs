using Cvitlo.Auth.Infrastructure.Database;
using Cvitlo.Auth.PublicApi.Events;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cvitlo.Auth.Application.Sessions;

// Only rows older than MinAge are touched, so the sweep never races a request that is still running.
public sealed class PendingErasureSweeper(
    AuthDbContext db,
    KeycloakAdminClient keycloak,
    PostgresTicketStore sessions,
    TimeProvider time,
    ILogger<PendingErasureSweeper> logger)
{
    public static readonly TimeSpan MinAge = TimeSpan.FromMinutes(10);

    // A user Keycloak still knows means the deletion never happened, so the row is dropped. Any failure keeps it for the next run.
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
