using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Languages.UpdateLanguage;

public class UpdateLanguageCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<UpdateLanguageCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> ExecuteAsync(UpdateLanguageCommand command, CancellationToken ct)
    {
        var language = await dbContext.Languages
            .FirstOrDefaultAsync(e => e.Id == command.Id && e.ResumeId == command.ResumeId, ct);

        if (language is null)
        {
            return Error.NotFound("Language.NotFound", $"Language with id '{command.Id}' was not found");
        }

        language.Name = command.Name;
        language.Level = command.Level;

        await dbContext.SaveChangesAsync(ct);

        return true;
    }
}