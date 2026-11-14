using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Educations.GetEducation;

public class GetEducationCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<GetEducationCommand, ErrorOr<GetEducationResponse>>
{
    public async Task<ErrorOr<GetEducationResponse>> ExecuteAsync(GetEducationCommand command, CancellationToken ct)
    {
        var education = await dbContext.Educations
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == command.Id && e.ResumeId == command.ResumeId, ct);

        if (education is null)
        {
            return Error.NotFound("Education.NotFound", $"Education with id '{command.Id}' was not found");
        }

        return education.MapToResponse();
    }
}