using ErrorOr;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Experiences.ReorderExperiences;

public class ReorderExperiencesCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<ReorderExperiencesCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> ExecuteAsync(ReorderExperiencesCommand command, CancellationToken ct)
    {
        var result = await SectionReorder.ApplyAsync(
            dbContext,
            dbContext.Experiences,
            command.ResumeId,
            command.OrderedIds,
            ct);

        if (result.IsError)
        {
            return result;
        }

        return result;
    }
}
