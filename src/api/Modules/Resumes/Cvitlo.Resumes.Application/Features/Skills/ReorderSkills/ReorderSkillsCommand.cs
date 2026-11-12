using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Skills.ReorderSkills;

public record ReorderSkillsCommand(
    Guid ResumeId,
    IReadOnlyList<Guid> OrderedIds) : ICommand<ErrorOr<bool>>;
