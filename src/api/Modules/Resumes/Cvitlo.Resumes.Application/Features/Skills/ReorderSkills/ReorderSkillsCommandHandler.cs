using ErrorOr;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Skills.ReorderSkills;

public class ReorderSkillsCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<ReorderSkillsCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> ExecuteAsync(ReorderSkillsCommand command, CancellationToken ct)
    {
        var result = await SectionReorder.ApplyAsync(
            dbContext,
            dbContext.Skills,
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
