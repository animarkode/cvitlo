using ErrorOr;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Resumes.UpdateSummary;

public class UpdateSummaryCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<UpdateSummaryCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> ExecuteAsync(UpdateSummaryCommand command, CancellationToken ct)
    {
        var resume = await dbContext.Resumes
            .FirstOrDefaultAsync(r => r.Id == command.Id && r.UserId == command.UserId, ct);

        if (resume is null)
        {
            return Error.NotFound("Resume.NotFound", $"Resume with id '{command.Id}' was not found");
        }

        var sanitized = HtmlContentSanitizer.SanitizeBasicFormatting(command.Summary);
        resume.Summary = string.IsNullOrWhiteSpace(sanitized) ? null : sanitized;
        await dbContext.SaveChangesAsync(ct);

        return true;
    }
}
