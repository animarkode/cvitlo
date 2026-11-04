namespace Faber.Modules.Auth.Application.Features.Account.GetLinkedAccounts;

/// <summary>Describes an identity provider and its link to the user's account.</summary>
/// <param name="ProviderAlias">The identity provider's alias.</param>
/// <param name="DisplayName">The identity provider's display name.</param>
/// <param name="Connected">Whether the provider is linked.</param>
/// <param name="LinkedUsername">The username associated with the linked account.</param>
/// <param name="LinkAction">App action name that links the provider (<c>link-google</c>, <c>link-github</c>); null otherwise.</param>
public record GetLinkedAccountsResponse(
    string ProviderAlias,
    string? DisplayName,
    bool Connected,
    string? LinkedUsername,
    string? LinkAction);
