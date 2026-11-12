using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Auth.Application.Features.BackchannelLogout;

public record BackchannelLogoutCommand(string LogoutToken) : ICommand<ErrorOr<Success>>;
