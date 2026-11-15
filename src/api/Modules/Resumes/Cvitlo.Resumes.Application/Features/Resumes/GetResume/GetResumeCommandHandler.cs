using ErrorOr;
using Cvitlo.Resumes.Application.Caching;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace Cvitlo.Resumes.Application.Features.Resumes.GetResume;

public class GetResumeCommandHandler(
    IServiceScopeFactory scopeFactory,
    HybridCache cache)
    : ICommandHandler<GetResumeCommand, ErrorOr<ResumeResponse>>
{
    public async Task<ErrorOr<ResumeResponse>> ExecuteAsync(GetResumeCommand command, CancellationToken ct)
    {
        var response = await cache.GetOrCreateAsync(
            ResumesCacheKeys.Resume(command.UserId, command.Id),
            async cacheCt =>
            {
                // HybridCache shares this factory across concurrent callers, so it must own its scope:
                // the first caller's request-scoped DbContext can be disposed under the others.
                await using var scope = scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ResumesDbContext>();

                var resume = await dbContext.Resumes
                    .AsNoTracking()
                    .Include(r => r.Person)
                    .Include(r => r.Experiences.OrderBy(e => e.Order))
                    .Include(r => r.Educations.OrderBy(e => e.Order))
                    .Include(r => r.Skills.OrderBy(s => s.Order))
                    .Include(r => r.Languages.OrderBy(l => l.Order))
                    .Include(r => r.Courses.OrderBy(c => c.Order))
                    .Include(r => r.Projects.OrderBy(p => p.Order))
                    .Include(r => r.Links.OrderBy(l => l.Order))
                    .FirstOrDefaultAsync(r => r.Id == command.Id && r.UserId == command.UserId, cacheCt);

                return resume?.ToResponse();
            },
            tags: [ResumesCacheKeys.ResumeTag(command.Id)],
            cancellationToken: ct);

        if (response is null)
        {
            return Error.NotFound("Resume.NotFound", $"Resume with id '{command.Id}' was not found");
        }

        return response;
    }
}
