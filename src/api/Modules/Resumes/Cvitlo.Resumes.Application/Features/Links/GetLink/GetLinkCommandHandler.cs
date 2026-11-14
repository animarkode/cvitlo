using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Links.GetLink;

public class GetLinkCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<GetLinkCommand, ErrorOr<GetLinkResponse>>
{
    public async Task<ErrorOr<GetLinkResponse>> ExecuteAsync(GetLinkCommand command, CancellationToken ct)
    {
        var link = await dbContext.Links
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == command.Id && e.ResumeId == command.ResumeId, ct);

        if (link is null)
        {
            return Error.NotFound("Link.NotFound", $"Link with id '{command.Id}' was not found");
        }

        return link.MapToResponse();
    }
}