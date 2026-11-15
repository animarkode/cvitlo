using Cvitlo.Auth.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Auth.Application.Sessions;

// Most sessions are never signed out, so without this their rows (encrypted refresh tokens included) stay forever.
public sealed class ExpiredSessionSweeper(AuthDbContext db, TimeProvider time)
{
    public Task<int> SweepAsync(CancellationToken ct)
    {
        var now = time.GetUtcNow();
        return db.Sessions.Where(session => session.ExpiresAt < now).ExecuteDeleteAsync(ct);
    }
}
