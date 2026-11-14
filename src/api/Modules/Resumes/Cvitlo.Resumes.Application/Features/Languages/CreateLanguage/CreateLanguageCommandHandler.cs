using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Languages.CreateLanguage;

public class CreateLanguageCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<CreateLanguageCommand, ErrorOr<CreateLanguageResponse>>
{
    public async Task<ErrorOr<CreateLanguageResponse>> ExecuteAsync(CreateLanguageCommand command, CancellationToken ct)
    {
        var resume = await dbContext.Resumes
            .FirstOrDefaultAsync(r => r.Id == command.ResumeId, ct);

        if (resume is null)
        {
            return Error.NotFound("Resume.NotFound", $"Resume with id '{command.ResumeId}' was not found");
        }

        var maxOrder = await dbContext.Languages
            .Where(l => l.ResumeId == command.ResumeId)
            .MaxAsync(l => (int?)l.Order, ct);

        var language = new Domain.Entities.Language
        {
            Id = Guid.NewGuid(),
            ResumeId = command.ResumeId,
            Name = command.Name,
            Level = command.Level,
            Order = (maxOrder ?? -1) + 1
        };

        dbContext.Languages.Add(language);
        await dbContext.SaveChangesAsync(ct);

        return language.MapToResponse();
    }
}