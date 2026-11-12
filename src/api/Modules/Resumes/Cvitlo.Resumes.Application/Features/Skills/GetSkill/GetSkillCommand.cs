using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Skills.GetSkill;

public record GetSkillCommand(Guid ResumeId, Guid Id) : ICommand<ErrorOr<GetSkillResponse>>;