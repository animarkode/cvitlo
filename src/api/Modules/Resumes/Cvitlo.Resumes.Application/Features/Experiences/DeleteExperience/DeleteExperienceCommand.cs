using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Experiences.DeleteExperience;

public record DeleteExperienceCommand(Guid ResumeId, Guid Id) : ICommand<ErrorOr<bool>>;