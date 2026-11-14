using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Resumes.DeleteResume;

public class DeleteResumeCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<DeleteResumeCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> ExecuteAsync(DeleteResumeCommand command, CancellationToken ct)
    {
        var resume = await dbContext.Resumes
            .FirstOrDefaultAsync(r => r.Id == command.Id && r.UserId == command.UserId, ct);

        if (resume is null)
        {
            return Error.NotFound("Resume.NotFound", $"Resume with id '{command.Id}' was not found");
        }

        dbContext.Resumes.Remove(resume);
        await dbContext.SaveChangesAsync(ct);

        return true;
    }
}