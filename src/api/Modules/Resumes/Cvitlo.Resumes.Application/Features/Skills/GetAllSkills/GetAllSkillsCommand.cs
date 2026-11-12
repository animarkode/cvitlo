using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Skills.GetAllSkills;

public record GetAllSkillsCommand(Guid ResumeId) : ICommand<ErrorOr<GetAllSkillsResponse>>;