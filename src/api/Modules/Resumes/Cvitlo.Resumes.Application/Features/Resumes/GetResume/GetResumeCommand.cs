using ErrorOr;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Resumes.GetResume;

public record GetResumeCommand(Guid Id, Guid UserId) : ICommand<ErrorOr<ResumeResponse>>;
