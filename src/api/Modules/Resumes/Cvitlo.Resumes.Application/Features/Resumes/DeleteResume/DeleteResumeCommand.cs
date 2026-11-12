using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Resumes.DeleteResume;

public record DeleteResumeCommand(Guid Id, Guid UserId) : ICommand<ErrorOr<bool>>;