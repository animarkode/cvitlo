using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Projects.GetProject;

public class GetProjectCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<GetProjectCommand, ErrorOr<GetProjectResponse>>
{
    public async Task<ErrorOr<GetProjectResponse>> ExecuteAsync(GetProjectCommand command, CancellationToken ct)
    {
        var project = await dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == command.Id && c.ResumeId == command.ResumeId, ct);

        if (project is null)
        {
            return Error.NotFound("Project.NotFound", $"Project with id '{command.Id}' was not found");
        }

        return project.MapToResponse();
    }
}