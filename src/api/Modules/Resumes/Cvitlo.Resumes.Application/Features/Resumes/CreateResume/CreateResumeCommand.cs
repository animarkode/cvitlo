using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Resumes.CreateResume;

public record CreateResumeCommand(Guid UserId, string? Localization) : ICommand<ErrorOr<CreateResumeResponse>>;