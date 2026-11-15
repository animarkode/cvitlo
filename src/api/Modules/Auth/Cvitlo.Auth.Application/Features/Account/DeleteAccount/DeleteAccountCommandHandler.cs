using System.Diagnostics.CodeAnalysis;
using ErrorOr;
using Cvitlo.Auth.Application.Sessions;
using Cvitlo.Auth.PublicApi;
using Cvitlo.Auth.Infrastructure.Database;
using Cvitlo.Auth.PublicApi.Events;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Cvitlo.Auth.Application.Features.Account.DeleteAccount;

// Order and failure handling: docs/decisions/account-deletion.md.
public class DeleteAccountCommandHandler(
    KeycloakAdminClient keycloak,
    AuthDbContext db,
    PostgresTicketStore sessions,
    TimeProvider time,
    ILogger<DeleteAccountCommandHandler> logger)
    : ICommandHandler<DeleteAccountCommand, ErrorOr<Deleted>>
{
    public static readonly TimeSpan MaxSignInAge = TimeSpan.FromMinutes(5);

    public async Task<ErrorOr<Deleted>> ExecuteAsync(DeleteAccountCommand command, CancellationToken ct)
    {
        if (!SignedInRecently(command.IdToken))
        {
            return Error.Forbidden(
                "Account.ReauthenticationRequired",
                "Sign in again to delete the account.");
        }

        await RecordPendingErasureAsync(command.Subject, ct);

        var deleted = await keycloak.DeleteUserAsync(command.Subject, ct);
        if (deleted.IsError)
        {
            // The row stays: a timeout can hide a deletion Keycloak did make.
            return deleted.FirstError;
        }

        // Past the point of no return: nothing below may depend on the request being alive.
        await sessions.RemoveBySubjectAsync(command.Subject);
        if (await EraseUserDataAsync(command.Subject))
        {
            await ForgetPendingErasureAsync(command.Subject);
        }

        return Result.Deleted;
    }

    // auth_time comes from the ID token Keycloak issued over the back channel, so the browser cannot choose it;
    // a refresh keeps it and only a new sign-in changes it.
    private bool SignedInRecently(string? idToken)
    {
        if (string.IsNullOrEmpty(idToken))
        {
            return false;
        }

        var token = new JsonWebToken(idToken);
        if (!token.TryGetPayloadValue<long>(JwtClaimTypes.Registered.AuthTime, out var authTime))
        {
            return false;
        }

        var age = time.GetUtcNow() - DateTimeOffset.FromUnixTimeSeconds(authTime);

        return age <= MaxSignInAge;
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "The Keycloak user is already deleted, so a failing module must not turn the request into an error; it is logged for a republish.")]
    private async Task<bool> EraseUserDataAsync(string subject)
    {
        try
        {
            await new UserDeleted(subject).PublishAsync(Mode.WaitForAll, CancellationToken.None);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "The data of {Subject} was not erased; the pending erasure is kept for the hourly sweep",
                subject);
            return false;
        }
    }

    private async Task RecordPendingErasureAsync(string subject, CancellationToken ct)
    {
        var pending = await db.PendingAccountErasures.AsTracking().FirstOrDefaultAsync(e => e.Subject == subject, ct);
        if (pending is null)
        {
            db.PendingAccountErasures.Add(new PendingAccountErasure { Subject = subject, RequestedAt = time.GetUtcNow() });
        }
        else
        {
            pending.RequestedAt = time.GetUtcNow();
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task ForgetPendingErasureAsync(string subject)
    {
        await db.PendingAccountErasures.Where(e => e.Subject == subject).ExecuteDeleteAsync(CancellationToken.None);
    }
}
