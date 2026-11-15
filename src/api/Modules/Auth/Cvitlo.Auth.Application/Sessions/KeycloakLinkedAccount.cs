namespace Cvitlo.Auth.Application.Sessions;

public sealed record KeycloakLinkedAccount(
    string ProviderAlias,
    string? DisplayName,
    bool Connected,
    string? LinkedUsername);
