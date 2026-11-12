using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Resumes.UpdateHobbies;

public record UpdateHobbiesCommand(Guid Id, Guid UserId, string? Hobbies) : ICommand<ErrorOr<bool>>;