using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Skills.CreateSkill;

public record CreateSkillCommand(
    Guid ResumeId,
    string? Name,
    string? Level) : ICommand<ErrorOr<CreateSkillResponse>>;