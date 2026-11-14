using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Skills.GetAllSkills;

public class GetAllSkillsCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<GetAllSkillsCommand, ErrorOr<GetAllSkillsResponse>>
{
    public async Task<ErrorOr<GetAllSkillsResponse>> ExecuteAsync(GetAllSkillsCommand command, CancellationToken ct)
    {
        var resume = await dbContext.Resumes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == command.ResumeId, ct);

        if (resume is null)
        {
            return Error.NotFound("Resume.NotFound", $"Resume with id '{command.ResumeId}' was not found");
        }

        var skills = await dbContext.Skills
            .AsNoTracking()
            .Where(s => s.ResumeId == command.ResumeId)
            .OrderBy(s => s.Order)
            .ToListAsync(ct);

        var items = skills.Select(s => s.MapToItem()).ToList();

        return new GetAllSkillsResponse(items);
    }
}