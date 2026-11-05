using System.Diagnostics.CodeAnalysis;
using ErrorOr;
using Faber.Modules.Auth.Application.Sessions;
using Faber.Modules.Auth.PublicApi;
using Faber.Modules.Auth.Infrastructure.Database;
using Faber.Modules.Auth.PublicApi.Events;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Faber.Modules.Auth.Application.Features.Account.DeleteAccount;

/// <summary>
/// Deletes the Keycloak user, tells the other modules to erase the user's data, and ends every web session.
/// </summary>
/// <remarks>
/// The order matters. Keycloak goes first: if it fails nothing else has happened and the person can retry. Once
/// the Keycloak user is gone the deletion cannot be undone, so the rest no longer depends on the request:
/// it runs with <see cref="CancellationToken.None"/>. Sessions end before <see cref="UserDeleted"/> is published,
/// so no other tab or device can write data after the erasure. A failing module does not fail the request.
/// A <see cref="PendingAccountErasure"/> row is saved before the Keycloak call and removed only when everything
/// is done, so a failed publish or a crash leaves a durable record that <see cref="PendingErasureSweeper"/>
/// finishes. If the Keycloak call fails the row stays too: a timeout can hide a deletion that did happen, and the
/// sweep checks whether the user still exists before it erases anything.
/// </remarks>
public class DeleteAccountCommandHandler(
    KeycloakAdminClient keycloak,
    AuthDbContext db,
    PostgresTicketStore sessions,
    TimeProvider time,
    ILogger<DeleteAccountCommandHandler> logger)
    : ICommandHandler<DeleteAccountCommand, ErrorOr<Deleted>>
{
    private const string HandlerName = nameof(DeleteAccountCommandHandler);

    /// <summary>The longest ago the person may have typed their credentials and still delete the account.</summary>
    public static readonly TimeSpan MaxSignInAge = TimeSpan.FromMinutes(5);

    public async Task<ErrorOr<Deleted>> ExecuteAsync(DeleteAccountCommand command, CancellationToken ct)
    {
        logger.LogInformation("[START] {HandlerName}", HandlerName);

        if (!SignedInRecently(command.IdToken))
        {
            logger.LogWarning("[FAIL] {HandlerName} | The last sign-in is too old", HandlerName);
            return Error.Forbidden(
                "Account.ReauthenticationRequired",
                "Sign in again to delete the account.");
        }

        await RecordPendingErasureAsync(command.Subject, ct);

        var deleted = await keycloak.DeleteUserAsync(command.Subject, ct);
        if (deleted.IsError)
        {
            // The row stays: a timeout can hide a deletion Keycloak did make. The sweep asks Keycloak and
            // forgets the row if the user still exists.
            logger.LogWarning("[FAIL] {HandlerName} | {Error}", HandlerName, deleted.FirstError.Code);
            return deleted.FirstError;
        }

        // Past the point of no return: a cancelled request must not leave sessions open or data behind.
        await sessions.RemoveBySubjectAsync(command.Subject);
        if (await EraseUserDataAsync(command.Subject))
        {
            await ForgetPendingErasureAsync(command.Subject);
        }

        logger.LogInformation("[SUCCESS] {HandlerName}", HandlerName);
        return Result.Deleted;
    }

    /// <summary>
    /// <c>auth_time</c> comes from the ID token Keycloak itself issued to the API over the back channel and the
    /// session stored, so the browser cannot choose it. Keycloak keeps it when a token is refreshed and sets a
    /// new one only when the person authenticates again.
    /// </summary>
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
                "[FAIL] {HandlerName} | The data of {Subject} was not erased; the pending erasure is kept for the hourly sweep",
                HandlerName,
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
