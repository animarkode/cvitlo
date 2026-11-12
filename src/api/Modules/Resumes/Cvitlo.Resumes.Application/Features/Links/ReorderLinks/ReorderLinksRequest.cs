namespace Cvitlo.Resumes.Application.Features.Links.ReorderLinks;

public record ReorderLinksRequest(
    Guid ResumeId,
    IReadOnlyList<Guid> OrderedIds);
