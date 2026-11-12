using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Projects.GetAllProjects;

public record GetAllProjectsCommand(Guid ResumeId) : ICommand<ErrorOr<GetAllProjectsResponse>>;