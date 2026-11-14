using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Projects.GetAllProjects;

public class GetAllProjectsCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<GetAllProjectsCommand, ErrorOr<GetAllProjectsResponse>>
{
    public async Task<ErrorOr<GetAllProjectsResponse>> ExecuteAsync(GetAllProjectsCommand command, CancellationToken ct)
    {
        var resume = await dbContext.Resumes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == command.ResumeId, ct);

        if (resume is null)
        {
            return Error.NotFound("Resume.NotFound", $"Resume with id '{command.ResumeId}' was not found");
        }

        var projects = await dbContext.Projects
            .AsNoTracking()
            .Where(c => c.ResumeId == command.ResumeId)
            .OrderBy(c => c.Order)
            .ToListAsync(ct);

        var items = projects.Select(e => e.MapToItem()).ToList();

        return new GetAllProjectsResponse(items);
    }
}