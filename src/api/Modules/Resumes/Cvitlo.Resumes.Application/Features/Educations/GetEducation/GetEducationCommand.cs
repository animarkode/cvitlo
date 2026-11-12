using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Educations.GetEducation;

public record GetEducationCommand(Guid ResumeId, Guid Id) : ICommand<ErrorOr<GetEducationResponse>>;