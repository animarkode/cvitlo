using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Skills.DeleteSkill;

public class DeleteSkillCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<DeleteSkillCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> ExecuteAsync(DeleteSkillCommand command, CancellationToken ct)
    {
        var skill = await dbContext.Skills
            .FirstOrDefaultAsync(s => s.Id == command.Id && s.ResumeId == command.ResumeId, ct);

        if (skill is null)
        {
            return Error.NotFound("Skill.NotFound", $"Skill with id '{command.Id}' was not found");
        }

        dbContext.Skills.Remove(skill);
        await dbContext.SaveChangesAsync(ct);

        return true;
    }
}