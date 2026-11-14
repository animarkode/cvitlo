using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Projects.DeleteProject;

public class DeleteProjectCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<DeleteProjectCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> ExecuteAsync(DeleteProjectCommand command, CancellationToken ct)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(c => c.Id == command.Id && c.ResumeId == command.ResumeId, ct);

        if (project is null)
        {
            return Error.NotFound("Project.NotFound", $"Project with id '{command.Id}' was not found");
        }

        dbContext.Projects.Remove(project);
        await dbContext.SaveChangesAsync(ct);

        return true;
    }
}