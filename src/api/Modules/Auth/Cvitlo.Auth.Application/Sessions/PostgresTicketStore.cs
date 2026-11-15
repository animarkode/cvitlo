using System.Security.Cryptography;
using System.Text;
using Cvitlo.Auth.Infrastructure.Database;
using Cvitlo.Auth.PublicApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cvitlo.Auth.Application.Sessions;

// The cookie carries only a random key and rows are found by its SHA-256, so a leaked backup or replica yields no usable cookie.
// Tickets are Data-Protection-encrypted; deleting the row ends the session at once.
public sealed class PostgresTicketStore(
    IServiceScopeFactory scopes,
    IDataProtectionProvider dataProtection,
    TimeProvider time) : ITicketStore
{
    public const string SessionKeyItem = ".cvitlo.session";

    private readonly IDataProtector _protector = dataProtection.CreateProtector("Cvitlo.Auth.Sessions.v1");

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        db.Sessions.Add(new AuthSession
        {
            Id = HashKey(key),
            Subject = ticket.Principal.FindFirst(JwtClaimTypes.Registered.Sub)?.Value ?? string.Empty,
            KeycloakSessionId = ticket.Principal.FindFirst(JwtClaimTypes.Registered.Sid)?.Value,
            Ticket = Protect(ticket),
            CreatedAt = time.GetUtcNow(),
            ExpiresAt = ticket.Properties.ExpiresUtc
        });
        await db.SaveChangesAsync();

        return key;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var id = HashKey(key);
        var protectedTicket = Protect(ticket);
        await db.Sessions
            .Where(session => session.Id == id)
            .ExecuteUpdateAsync(set => set
                .SetProperty(session => session.Ticket, protectedTicket)
                .SetProperty(session => session.ExpiresAt, ticket.Properties.ExpiresUtc));
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var id = HashKey(key);
        var session = await db.Sessions.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id);

        if (session is null || session.ExpiresAt <= time.GetUtcNow())
        {
            return null;
        }

        return Unprotect(session.Ticket, key);
    }

    public async Task RemoveAsync(string key)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var id = HashKey(key);
        await db.Sessions.Where(session => session.Id == id).ExecuteDeleteAsync();
    }

    // The Keycloak session is left alone: its tokens are shared with the session that replaces this one.
    public async Task RemoveByIdAsync(string id, string? subject = null)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await db.Sessions
            .Where(session => session.Id == id && (subject == null || session.Subject == subject))
            .ExecuteDeleteAsync();
    }

    public async Task RemoveBySubjectAsync(string subject)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await db.Sessions.Where(session => session.Subject == subject).ExecuteDeleteAsync();
    }

    public static string HashKey(string key)
    {
        return WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
    }

    public byte[] Protect(AuthenticationTicket ticket)
    {
        ticket.Properties.Items.Remove(SessionKeyItem);
        return _protector.Protect(TicketSerializer.Default.Serialize(ticket));
    }

    public AuthenticationTicket? Unprotect(byte[] protectedTicket, string key)
    {
        try
        {
            var ticket = TicketSerializer.Default.Deserialize(_protector.Unprotect(protectedTicket));
            if (ticket is not null)
            {
                ticket.Properties.Items[SessionKeyItem] = key;
            }

            return ticket;
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
