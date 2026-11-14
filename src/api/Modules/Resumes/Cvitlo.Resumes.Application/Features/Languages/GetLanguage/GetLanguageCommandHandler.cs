using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Languages.GetLanguage;

public class GetLanguageCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<GetLanguageCommand, ErrorOr<GetLanguageResponse>>
{
    public async Task<ErrorOr<GetLanguageResponse>> ExecuteAsync(GetLanguageCommand command, CancellationToken ct)
    {
        var language = await dbContext.Languages
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == command.Id && e.ResumeId == command.ResumeId, ct);

        if (language is null)
        {
            return Error.NotFound("Language.NotFound", $"Language with id '{command.Id}' was not found");
        }

        return language.MapToResponse();
    }
}