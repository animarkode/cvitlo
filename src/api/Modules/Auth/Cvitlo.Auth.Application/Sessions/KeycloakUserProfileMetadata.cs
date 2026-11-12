namespace Cvitlo.Auth.Application.Sessions;

public sealed record KeycloakUserProfileMetadata(IReadOnlyList<KeycloakProfileField>? Attributes);
