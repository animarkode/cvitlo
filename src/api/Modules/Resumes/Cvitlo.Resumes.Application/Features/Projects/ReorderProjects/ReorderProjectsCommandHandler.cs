using ErrorOr;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Projects.ReorderProjects;

public class ReorderProjectsCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<ReorderProjectsCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> ExecuteAsync(ReorderProjectsCommand command, CancellationToken ct)
    {
        var result = await SectionReorder.ApplyAsync(
            dbContext,
            dbContext.Projects,
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
