using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Links.CreateLink;

public class CreateLinkCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<CreateLinkCommand, ErrorOr<CreateLinkResponse>>
{
    public async Task<ErrorOr<CreateLinkResponse>> ExecuteAsync(CreateLinkCommand command, CancellationToken ct)
    {
        var resume = await dbContext.Resumes
            .FirstOrDefaultAsync(r => r.Id == command.ResumeId, ct);

        if (resume is null)
        {
            return Error.NotFound("Resume.NotFound", $"Resume with id '{command.ResumeId}' was not found");
        }

        var maxOrder = await dbContext.Links
            .Where(l => l.ResumeId == command.ResumeId)
            .MaxAsync(l => (int?)l.Order, ct);

        var link = new Domain.Entities.Link
        {
            Id = Guid.NewGuid(),
            ResumeId = command.ResumeId,
            Label = command.Label,
            Uri = command.Uri,
            Order = (maxOrder ?? -1) + 1
        };

        dbContext.Links.Add(link);
        await dbContext.SaveChangesAsync(ct);

        return link.MapToResponse();
    }
}