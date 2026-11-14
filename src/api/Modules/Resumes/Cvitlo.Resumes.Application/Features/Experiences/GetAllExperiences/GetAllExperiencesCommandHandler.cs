using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Experiences.GetAllExperiences;

public class GetAllExperiencesCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<GetAllExperiencesCommand, ErrorOr<GetAllExperiencesResponse>>
{
    public async Task<ErrorOr<GetAllExperiencesResponse>> ExecuteAsync(
        GetAllExperiencesCommand command,
        CancellationToken ct)
    {
        var resume = await dbContext.Resumes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == command.ResumeId, ct);

        if (resume is null)
        {
            return Error.NotFound("Resume.NotFound", $"Resume with id '{command.ResumeId}' was not found");
        }

        var experiences = await dbContext.Experiences
            .AsNoTracking()
            .Where(e => e.ResumeId == command.ResumeId)
            .OrderBy(e => e.Order)
            .ToListAsync(ct);

        var items = experiences.Select(e => e.MapToItem()).ToList();

        return new GetAllExperiencesResponse(items);
    }
}