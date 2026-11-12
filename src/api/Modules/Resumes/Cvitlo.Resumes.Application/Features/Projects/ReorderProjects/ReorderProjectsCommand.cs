using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Projects.ReorderProjects;

public record ReorderProjectsCommand(
    Guid ResumeId,
    IReadOnlyList<Guid> OrderedIds) : ICommand<ErrorOr<bool>>;
