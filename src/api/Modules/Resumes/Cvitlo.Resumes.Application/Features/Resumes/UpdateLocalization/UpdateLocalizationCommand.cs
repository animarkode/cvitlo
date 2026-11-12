using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Resumes.UpdateLocalization;

public record UpdateLocalizationCommand(Guid Id, Guid UserId, string? Localization) : ICommand<ErrorOr<bool>>;