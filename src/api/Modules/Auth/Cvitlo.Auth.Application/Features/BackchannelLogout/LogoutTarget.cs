namespace Cvitlo.Auth.Application.Features.BackchannelLogout;

public record LogoutTarget(string? KeycloakSessionId, string? Subject);
