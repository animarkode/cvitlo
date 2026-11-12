using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Links.ReorderLinks;

public record ReorderLinksCommand(
    Guid ResumeId,
    IReadOnlyList<Guid> OrderedIds) : ICommand<ErrorOr<bool>>;
