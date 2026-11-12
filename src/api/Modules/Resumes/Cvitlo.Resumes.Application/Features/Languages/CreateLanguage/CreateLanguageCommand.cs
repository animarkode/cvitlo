using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Languages.CreateLanguage;

public record CreateLanguageCommand(
    Guid ResumeId,
    string? Name,
    string? Level) : ICommand<ErrorOr<CreateLanguageResponse>>;