using Cvitlo.Auth.PublicApi.Events;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cvitlo.Resumes.Application.Features.Resumes.DeleteUserResumes;

// Singleton handler, so the DbContext comes from a fresh scope. Removal is tracked so the cache
// interceptor sees the deleted resumes; FK cascade removes the child rows. A repeat finds nothing to do.
public class UserDeletedHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<UserDeletedHandler> logger)
    : IEventHandler<UserDeleted>
{
    public async Task HandleAsync(UserDeleted eventModel, CancellationToken ct)
    {
        if (!Guid.TryParse(eventModel.Subject, out var userId))
        {
            logger.LogWarning("Skipping resume erasure: the event subject is not a valid user id");

            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ResumesDbContext>();

        var resumes = await dbContext.Resumes
            .AsTracking()
            .Where(r => r.UserId == userId)
            .ToListAsync(ct);

        if (resumes.Count == 0)
        {
            logger.LogInformation("User {UserId} has no resumes to erase", userId);

            return;
        }

        dbContext.Resumes.RemoveRange(resumes);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Erased {Count} resumes of user {UserId}", resumes.Count, userId);
    }
}
