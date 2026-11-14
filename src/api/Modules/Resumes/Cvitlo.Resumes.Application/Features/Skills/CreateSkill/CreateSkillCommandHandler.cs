using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Skills.CreateSkill;

public class CreateSkillCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<CreateSkillCommand, ErrorOr<CreateSkillResponse>>
{
    public async Task<ErrorOr<CreateSkillResponse>> ExecuteAsync(CreateSkillCommand command, CancellationToken ct)
    {
        var resume = await dbContext.Resumes
            .FirstOrDefaultAsync(r => r.Id == command.ResumeId, ct);

        if (resume is null)
        {
            return Error.NotFound("Resume.NotFound", $"Resume with id '{command.ResumeId}' was not found");
        }

        var maxOrder = await dbContext.Skills
            .Where(s => s.ResumeId == command.ResumeId)
            .MaxAsync(s => (int?)s.Order, ct);

        var skill = new Domain.Entities.Skill
        {
            Id = Guid.NewGuid(),
            ResumeId = command.ResumeId,
            Name = command.Name,
            Level = command.Level,
            Order = (maxOrder ?? -1) + 1
        };

        dbContext.Skills.Add(skill);
        await dbContext.SaveChangesAsync(ct);

        return skill.MapToResponse();
    }
}