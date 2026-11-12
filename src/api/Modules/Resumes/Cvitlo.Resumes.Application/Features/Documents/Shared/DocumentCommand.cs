using ErrorOr;
using Cvitlo.Resumes.Domain.Entities;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Documents.Shared;

public record DocumentCommand(Guid ResumeId, Guid UserId) : ICommand<ErrorOr<Resume>>;