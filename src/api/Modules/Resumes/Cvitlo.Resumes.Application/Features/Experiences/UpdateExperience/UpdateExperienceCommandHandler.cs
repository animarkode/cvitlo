using ErrorOr;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Experiences.UpdateExperience;

public class UpdateExperienceCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<UpdateExperienceCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> ExecuteAsync(UpdateExperienceCommand command, CancellationToken ct)
    {
        var experience = await dbContext.Experiences
            .FirstOrDefaultAsync(e => e.Id == command.Id && e.ResumeId == command.ResumeId, ct);

        if (experience is null)
        {
            return Error.NotFound(
                "Experience.NotFound",
                $"Experience with id '{command.Id}' was not found");
        }

        experience.JobTitle = command.JobTitle;
        experience.Employer = command.Employer;
        experience.StartDate = command.StartDate;
        experience.EndDate = command.EndDate;
        experience.City = command.City;
        experience.Description = HtmlContentSanitizer.SanitizeBasicFormatting(command.Description);

        await dbContext.SaveChangesAsync(ct);

        return true;
    }
}
