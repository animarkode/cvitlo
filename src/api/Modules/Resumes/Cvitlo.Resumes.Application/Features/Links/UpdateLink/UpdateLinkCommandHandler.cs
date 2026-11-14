using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Links.UpdateLink;

public class UpdateLinkCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<UpdateLinkCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> ExecuteAsync(UpdateLinkCommand command, CancellationToken ct)
    {
        var link = await dbContext.Links
            .FirstOrDefaultAsync(e => e.Id == command.Id && e.ResumeId == command.ResumeId, ct);

        if (link is null)
        {
            return Error.NotFound("Link.NotFound", $"Link with id '{command.Id}' was not found");
        }

        link.Label = command.Label;
        link.Uri = command.Uri;

        await dbContext.SaveChangesAsync(ct);

        return true;
    }
}
