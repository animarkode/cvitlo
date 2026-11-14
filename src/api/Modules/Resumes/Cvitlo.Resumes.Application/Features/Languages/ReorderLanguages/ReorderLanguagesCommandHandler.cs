using ErrorOr;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Languages.ReorderLanguages;

public class ReorderLanguagesCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<ReorderLanguagesCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> ExecuteAsync(ReorderLanguagesCommand command, CancellationToken ct)
    {
        var result = await SectionReorder.ApplyAsync(
            dbContext,
            dbContext.Languages,
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
