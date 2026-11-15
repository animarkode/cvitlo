namespace Cvitlo.Auth.Infrastructure.Database;

// Written before the Keycloak user is deleted and removed once sessions end and UserDeleted is published;
// a sweep finishes any row that outlives its request.
public class PendingAccountErasure
{
    public string Subject { get; set; } = string.Empty;

    public DateTimeOffset RequestedAt { get; set; }
}
