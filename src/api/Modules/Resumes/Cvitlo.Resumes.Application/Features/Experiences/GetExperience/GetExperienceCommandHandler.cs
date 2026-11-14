using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Experiences.GetExperience;

public class GetExperienceCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<GetExperienceCommand, ErrorOr<GetExperienceResponse>>
{
    public async Task<ErrorOr<GetExperienceResponse>> ExecuteAsync(
        GetExperienceCommand command,
        CancellationToken ct)
    {
        var experience = await dbContext.Experiences
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == command.Id && e.ResumeId == command.ResumeId, ct);

        if (experience is null)
        {
            return Error.NotFound(
                "Experience.NotFound",
                $"Experience with id '{command.Id}' was not found");
        }

        return experience.MapToResponse();
    }
}