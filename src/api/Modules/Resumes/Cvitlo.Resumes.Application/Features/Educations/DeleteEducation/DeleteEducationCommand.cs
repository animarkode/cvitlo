using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Educations.DeleteEducation;

public record DeleteEducationCommand(Guid ResumeId, Guid Id) : ICommand<ErrorOr<bool>>;