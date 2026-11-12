using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Languages.GetLanguage;

public record GetLanguageCommand(Guid ResumeId, Guid Id) : ICommand<ErrorOr<GetLanguageResponse>>;