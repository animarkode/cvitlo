using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Languages.DeleteLanguage;

public class DeleteLanguageCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<DeleteLanguageCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> ExecuteAsync(DeleteLanguageCommand command, CancellationToken ct)
    {
        var language = await dbContext.Languages
            .FirstOrDefaultAsync(e => e.Id == command.Id && e.ResumeId == command.ResumeId, ct);

        if (language is null)
        {
            return Error.NotFound("Language.NotFound", $"Language with id '{command.Id}' was not found");
        }

        dbContext.Languages.Remove(language);
        await dbContext.SaveChangesAsync(ct);

        return true;
    }
}