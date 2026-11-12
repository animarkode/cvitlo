using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Resumes.GetAllResumes;

public record GetAllResumesCommand(Guid UserId) : ICommand<ErrorOr<GetAllResumesResponse>>;