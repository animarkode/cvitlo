using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Experiences.DeleteExperience;

public class DeleteExperienceCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<DeleteExperienceCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> ExecuteAsync(DeleteExperienceCommand command, CancellationToken ct)
    {
        var experience = await dbContext.Experiences
            .FirstOrDefaultAsync(e => e.Id == command.Id && e.ResumeId == command.ResumeId, ct);

        if (experience is null)
        {
            return Error.NotFound(
                "Experience.NotFound",
                $"Experience with id '{command.Id}' was not found");
        }

        dbContext.Experiences.Remove(experience);
        await dbContext.SaveChangesAsync(ct);

        return true;
    }
}