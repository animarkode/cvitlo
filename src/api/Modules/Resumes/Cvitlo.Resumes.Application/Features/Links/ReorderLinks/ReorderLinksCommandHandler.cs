using ErrorOr;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Links.ReorderLinks;

public class ReorderLinksCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<ReorderLinksCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> ExecuteAsync(ReorderLinksCommand command, CancellationToken ct)
    {
        var result = await SectionReorder.ApplyAsync(
            dbContext,
            dbContext.Links,
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
