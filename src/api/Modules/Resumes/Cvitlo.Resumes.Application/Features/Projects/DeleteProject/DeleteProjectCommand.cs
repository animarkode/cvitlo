using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Projects.DeleteProject;

public record DeleteProjectCommand(Guid ResumeId, Guid Id) : ICommand<ErrorOr<bool>>;