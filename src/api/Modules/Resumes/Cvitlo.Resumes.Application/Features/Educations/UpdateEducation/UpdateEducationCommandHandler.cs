using ErrorOr;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Educations.UpdateEducation;

public class UpdateEducationCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<UpdateEducationCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> ExecuteAsync(UpdateEducationCommand command, CancellationToken ct)
    {
        var education = await dbContext.Educations
            .FirstOrDefaultAsync(e => e.Id == command.Id && e.ResumeId == command.ResumeId, ct);

        if (education is null)
        {
            return Error.NotFound("Education.NotFound", $"Education with id '{command.Id}' was not found");
        }

        education.School = command.School;
        education.Degree = command.Degree;
        education.StartDate = command.StartDate;
        education.EndDate = command.EndDate;
        education.City = command.City;
        education.Description = HtmlContentSanitizer.SanitizeBasicFormatting(command.Description);

        await dbContext.SaveChangesAsync(ct);

        return true;
    }
}
