using ErrorOr;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Educations.CreateEducation;

public class CreateEducationCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<CreateEducationCommand, ErrorOr<CreateEducationResponse>>
{
    public async Task<ErrorOr<CreateEducationResponse>> ExecuteAsync(
        CreateEducationCommand command,
        CancellationToken ct)
    {
        var resume = await dbContext.Resumes
            .FirstOrDefaultAsync(r => r.Id == command.ResumeId, ct);

        if (resume is null)
        {
            return Error.NotFound("Resume.NotFound", $"Resume with id '{command.ResumeId}' was not found");
        }

        var maxOrder = await dbContext.Educations
            .Where(e => e.ResumeId == command.ResumeId)
            .MaxAsync(e => (int?)e.Order, ct);

        var education = new Domain.Entities.Education
        {
            Id = Guid.NewGuid(),
            ResumeId = command.ResumeId,
            School = command.School,
            Degree = command.Degree,
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            City = command.City,
            Description = HtmlContentSanitizer.SanitizeBasicFormatting(command.Description),
            Order = (maxOrder ?? -1) + 1
        };

        dbContext.Educations.Add(education);
        await dbContext.SaveChangesAsync(ct);

        return education.MapToResponse();
    }
}