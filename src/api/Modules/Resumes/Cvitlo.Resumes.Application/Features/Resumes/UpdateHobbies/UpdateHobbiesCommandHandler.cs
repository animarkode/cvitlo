using ErrorOr;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Resumes.UpdateHobbies;

public class UpdateHobbiesCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<UpdateHobbiesCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> ExecuteAsync(UpdateHobbiesCommand command, CancellationToken ct)
    {
        var resume = await dbContext.Resumes
            .FirstOrDefaultAsync(r => r.Id == command.Id && r.UserId == command.UserId, ct);

        if (resume is null)
        {
            return Error.NotFound("Resume.NotFound", $"Resume with id '{command.Id}' was not found");
        }

        resume.Hobbies = HtmlContentSanitizer.SanitizeBasicFormatting(command.Hobbies);
        await dbContext.SaveChangesAsync(ct);

        return true;
    }
}
