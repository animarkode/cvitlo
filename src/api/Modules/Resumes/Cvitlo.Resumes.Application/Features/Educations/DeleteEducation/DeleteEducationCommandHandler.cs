using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Educations.DeleteEducation;

public class DeleteEducationCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<DeleteEducationCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> ExecuteAsync(DeleteEducationCommand command, CancellationToken ct)
    {
        var education = await dbContext.Educations
            .FirstOrDefaultAsync(e => e.Id == command.Id && e.ResumeId == command.ResumeId, ct);

        if (education is null)
        {
            return Error.NotFound("Education.NotFound", $"Education with id '{command.Id}' was not found");
        }

        dbContext.Educations.Remove(education);
        await dbContext.SaveChangesAsync(ct);

        return true;
    }
}