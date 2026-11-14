using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Skills.GetSkill;

public class GetSkillCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<GetSkillCommand, ErrorOr<GetSkillResponse>>
{
    public async Task<ErrorOr<GetSkillResponse>> ExecuteAsync(GetSkillCommand command, CancellationToken ct)
    {
        var skill = await dbContext.Skills
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == command.Id && s.ResumeId == command.ResumeId, ct);

        if (skill is null)
        {
            return Error.NotFound("Skill.NotFound", $"Skill with id '{command.Id}' was not found");
        }

        return skill.MapToResponse();
    }
}