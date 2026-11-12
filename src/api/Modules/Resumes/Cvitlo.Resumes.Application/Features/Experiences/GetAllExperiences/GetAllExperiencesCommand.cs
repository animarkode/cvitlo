using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Experiences.GetAllExperiences;

public record GetAllExperiencesCommand(Guid ResumeId)
    : ICommand<ErrorOr<GetAllExperiencesResponse>>;