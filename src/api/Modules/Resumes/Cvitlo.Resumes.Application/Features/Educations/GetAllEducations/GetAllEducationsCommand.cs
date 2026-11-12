using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Educations.GetAllEducations;

public record GetAllEducationsCommand(Guid ResumeId) : ICommand<ErrorOr<GetAllEducationsResponse>>;