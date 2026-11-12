using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Educations.ReorderEducations;

public record ReorderEducationsCommand(
    Guid ResumeId,
    IReadOnlyList<Guid> OrderedIds) : ICommand<ErrorOr<bool>>;
