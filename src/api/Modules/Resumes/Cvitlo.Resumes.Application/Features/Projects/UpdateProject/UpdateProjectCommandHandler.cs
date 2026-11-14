using ErrorOr;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Projects.UpdateProject;

public class UpdateProjectCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<UpdateProjectCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> ExecuteAsync(UpdateProjectCommand command, CancellationToken ct)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(c => c.Id == command.Id && c.ResumeId == command.ResumeId, ct);

        if (project is null)
        {
            return Error.NotFound("Project.NotFound", $"Project with id '{command.Id}' was not found");
        }

        project.Tagline = command.Tagline;
        project.Name = command.Name;
        project.Url = command.Url;
        project.StartDate = command.StartDate;
        project.EndDate = command.EndDate;
        project.Description = HtmlContentSanitizer.SanitizeBasicFormatting(command.Description);

        await dbContext.SaveChangesAsync(ct);

        return true;
    }
}
