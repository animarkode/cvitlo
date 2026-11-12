using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Resumes.UpdateSummary;

public record UpdateSummaryCommand(Guid Id, Guid UserId, string? Summary) : ICommand<ErrorOr<bool>>;