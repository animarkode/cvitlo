using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Projects.GetProject;

public record GetProjectCommand(Guid ResumeId, Guid Id) : ICommand<ErrorOr<GetProjectResponse>>;