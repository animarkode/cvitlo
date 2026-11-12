using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Languages.GetAllLanguages;

public record GetAllLanguagesCommand(Guid ResumeId) : ICommand<ErrorOr<GetAllLanguagesResponse>>;