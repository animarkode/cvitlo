using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Skills.UpdateSkill;

public class UpdateSkillCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<UpdateSkillCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> ExecuteAsync(UpdateSkillCommand command, CancellationToken ct)
    {
        var skill = await dbContext.Skills
            .FirstOrDefaultAsync(s => s.Id == command.Id && s.ResumeId == command.ResumeId, ct);

        if (skill is null)
        {
            return Error.NotFound("Skill.NotFound", $"Skill with id '{command.Id}' was not found");
        }

        skill.Name = command.Name;
        skill.Level = command.Level;

        await dbContext.SaveChangesAsync(ct);

        return true;
    }
}
