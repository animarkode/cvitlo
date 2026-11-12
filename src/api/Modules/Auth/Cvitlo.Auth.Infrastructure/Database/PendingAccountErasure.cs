namespace Cvitlo.Auth.Infrastructure.Database;

/// <summary>
/// An account deletion that has started and is not yet finished everywhere. The row is written before the
/// Keycloak user is deleted and removed once sessions are ended and <c>UserDeleted</c> is published; a row that
/// outlives its request is finished (or dropped, if Keycloak still knows the user) by a sweep.
/// </summary>
public class PendingAccountErasure
{
    /// <summary>Keycloak user id (<c>sub</c>) of the account being deleted.</summary>
    public string Subject { get; set; } = string.Empty;

    public DateTimeOffset RequestedAt { get; set; }
}
