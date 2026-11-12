using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Languages.ReorderLanguages;

public record ReorderLanguagesCommand(
    Guid ResumeId,
    IReadOnlyList<Guid> OrderedIds) : ICommand<ErrorOr<bool>>;
