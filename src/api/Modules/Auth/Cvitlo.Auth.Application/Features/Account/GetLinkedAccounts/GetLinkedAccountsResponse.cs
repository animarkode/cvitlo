namespace Cvitlo.Auth.Application.Features.Account.GetLinkedAccounts;

public record GetLinkedAccountsResponse(
    string ProviderAlias,
    string? DisplayName,
    bool Connected,
    string? LinkedUsername,
    string? LinkAction);
