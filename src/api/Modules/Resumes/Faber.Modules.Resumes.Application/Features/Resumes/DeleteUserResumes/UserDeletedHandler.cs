using Faber.Modules.Auth.PublicApi.Events;
using Faber.Modules.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Faber.Modules.Resumes.Application.Features.Resumes.DeleteUserResumes;

/// <summary>
/// Hard-deletes every resume owned by a user whose account was deleted.
/// </summary>
/// <remarks>
/// Event handlers are singletons, so the <see cref="ResumesDbContext"/> is resolved from a fresh scope.
/// Removal is tracked on purpose: database foreign keys cascade to the child rows and the cache
/// invalidation interceptor sees the deleted resumes. Running it again finds nothing and does nothing.
/// </remarks>
public class UserDeletedHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<UserDeletedHandler> logger)
    : IEventHandler<UserDeleted>
{
    private const string HandlerName = nameof(UserDeletedHandler);

    /// <inheritdoc />
    public async Task HandleAsync(UserDeleted eventModel, CancellationToken ct)
    {
        logger.LogInformation("[START] {HandlerName}", HandlerName);

        if (!Guid.TryParse(eventModel.Subject, out var userId))
        {
            logger.LogWarning("[FAIL] {HandlerName} | Subject is not a valid user id", HandlerName);

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
            logger.LogInformation("[SUCCESS] {HandlerName} | User {UserId} has no resumes", HandlerName, userId);

            return;
        }

        dbContext.Resumes.RemoveRange(resumes);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation(
            "[SUCCESS] {HandlerName} | {Count} resumes of user {UserId} deleted",
            HandlerName,
            resumes.Count,
            userId);
    }
}
