using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Links.GetAllLinks;

public class GetAllLinksCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<GetAllLinksCommand, ErrorOr<GetAllLinksResponse>>
{
    public async Task<ErrorOr<GetAllLinksResponse>> ExecuteAsync(GetAllLinksCommand command, CancellationToken ct)
    {
        var resume = await dbContext.Resumes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == command.ResumeId, ct);

        if (resume is null)
        {
            return Error.NotFound("Resume.NotFound", $"Resume with id '{command.ResumeId}' was not found");
        }

        var links = await dbContext.Links
            .AsNoTracking()
            .Where(e => e.ResumeId == command.ResumeId)
            .OrderBy(e => e.Order)
            .ToListAsync(ct);

        var items = links.Select(e => e.MapToItem()).ToList();

        return new GetAllLinksResponse(items);
    }
}