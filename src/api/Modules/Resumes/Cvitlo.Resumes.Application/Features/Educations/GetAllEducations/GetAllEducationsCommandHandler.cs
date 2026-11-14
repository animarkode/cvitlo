using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Educations.GetAllEducations;

public class GetAllEducationsCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<GetAllEducationsCommand, ErrorOr<GetAllEducationsResponse>>
{
    public async Task<ErrorOr<GetAllEducationsResponse>> ExecuteAsync(
        GetAllEducationsCommand command,
        CancellationToken ct)
    {
        var resume = await dbContext.Resumes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == command.ResumeId, ct);

        if (resume is null)
        {
            return Error.NotFound("Resume.NotFound", $"Resume with id '{command.ResumeId}' was not found");
        }

        var educations = await dbContext.Educations
            .AsNoTracking()
            .Where(e => e.ResumeId == command.ResumeId)
            .OrderBy(e => e.Order)
            .ToListAsync(ct);

        var items = educations.Select(e => e.MapToItem()).ToList();

        return new GetAllEducationsResponse(items);
    }
}