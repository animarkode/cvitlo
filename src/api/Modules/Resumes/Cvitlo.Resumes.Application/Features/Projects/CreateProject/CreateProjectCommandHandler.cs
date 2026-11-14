using ErrorOr;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Domain.Entities;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Projects.CreateProject;

public class CreateProjectCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<CreateProjectCommand, ErrorOr<CreateProjectResponse>>
{
    public async Task<ErrorOr<CreateProjectResponse>> ExecuteAsync(CreateProjectCommand command, CancellationToken ct)
    {
        var resume = await dbContext.Resumes
            .FirstOrDefaultAsync(r => r.Id == command.ResumeId, ct);

        if (resume is null)
        {
            return Error.NotFound("Resume.NotFound", $"Resume with id '{command.ResumeId}' was not found");
        }

        var maxOrder = await dbContext.Projects
            .Where(c => c.ResumeId == command.ResumeId)
            .MaxAsync(c => (int?)c.Order, ct);

        var project = new Project
        {
            Id = Guid.NewGuid(),
            ResumeId = command.ResumeId,
            Tagline = command.Tagline,
            Name = command.Name,
            Url = command.Url,
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            Description = HtmlContentSanitizer.SanitizeBasicFormatting(command.Description),
            Order = (maxOrder ?? -1) + 1
        };

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(ct);

        return project.MapToResponse();
    }
}