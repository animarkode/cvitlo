using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Links.DeleteLink;

public class DeleteLinkCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<DeleteLinkCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> ExecuteAsync(DeleteLinkCommand command, CancellationToken ct)
    {
        var link = await dbContext.Links
            .FirstOrDefaultAsync(e => e.Id == command.Id && e.ResumeId == command.ResumeId, ct);

        if (link is null)
        {
            return Error.NotFound("Link.NotFound", $"Link with id '{command.Id}' was not found");
        }

        dbContext.Links.Remove(link);
        await dbContext.SaveChangesAsync(ct);

        return true;
    }
}