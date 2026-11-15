using System.Diagnostics.CodeAnalysis;

namespace Cvitlo.Auth.Infrastructure.Database;

// Tokens live here, encrypted with Data Protection; the browser holds only the Id in its cookie.
public class AuthSession
{
    public string Id { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string? KeycloakSessionId { get; set; }

    [SuppressMessage("Performance", "CA1819:Properties should not return arrays",
        Justification = "EF Core maps this property to a bytea column, which requires byte[].")]
    public byte[] Ticket { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }
}
