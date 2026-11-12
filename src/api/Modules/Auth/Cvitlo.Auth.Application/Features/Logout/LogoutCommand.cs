using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Auth.Application.Features.Logout;

public record LogoutCommand(string? RefreshToken) : ICommand<ErrorOr<Success>>;
