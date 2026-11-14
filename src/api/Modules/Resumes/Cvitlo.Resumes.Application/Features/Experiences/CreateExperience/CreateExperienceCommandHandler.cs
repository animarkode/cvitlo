using ErrorOr;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Experiences.CreateExperience;

public class CreateExperienceCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<CreateExperienceCommand, ErrorOr<CreateExperienceResponse>>
{
    public async Task<ErrorOr<CreateExperienceResponse>> ExecuteAsync(
        CreateExperienceCommand command,
        CancellationToken ct)
    {
        var resume = await dbContext.Resumes
            .FirstOrDefaultAsync(r => r.Id == command.ResumeId, ct);

        if (resume is null)
        {
            return Error.NotFound("Resume.NotFound", $"Resume with id '{command.ResumeId}' was not found");
        }

        var maxOrder = await dbContext.Experiences
            .Where(e => e.ResumeId == command.ResumeId)
            .MaxAsync(e => (int?)e.Order, ct);

        var experience = new Domain.Entities.Experience
        {
            Id = Guid.NewGuid(),
            ResumeId = command.ResumeId,
            JobTitle = command.JobTitle,
            Employer = command.Employer,
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            City = command.City,
            Description = HtmlContentSanitizer.SanitizeBasicFormatting(command.Description),
            Order = (maxOrder ?? -1) + 1
        };

        dbContext.Experiences.Add(experience);
        await dbContext.SaveChangesAsync(ct);

        return experience.MapToResponse();
    }
}