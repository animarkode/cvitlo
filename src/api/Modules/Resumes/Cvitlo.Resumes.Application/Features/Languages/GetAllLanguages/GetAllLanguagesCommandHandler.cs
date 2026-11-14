using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Languages.GetAllLanguages;

public class GetAllLanguagesCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<GetAllLanguagesCommand, ErrorOr<GetAllLanguagesResponse>>
{
    public async Task<ErrorOr<GetAllLanguagesResponse>> ExecuteAsync(
        GetAllLanguagesCommand command,
        CancellationToken ct)
    {
        var resume = await dbContext.Resumes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == command.ResumeId, ct);

        if (resume is null)
        {
            return Error.NotFound("Resume.NotFound", $"Resume with id '{command.ResumeId}' was not found");
        }

        var languages = await dbContext.Languages
            .AsNoTracking()
            .Where(e => e.ResumeId == command.ResumeId)
            .OrderBy(e => e.Order)
            .ToListAsync(ct);

        var items = languages.Select(e => e.MapToItem()).ToList();

        return new GetAllLanguagesResponse(items);
    }
}