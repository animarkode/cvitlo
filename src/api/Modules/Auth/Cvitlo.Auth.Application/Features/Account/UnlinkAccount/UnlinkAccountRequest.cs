namespace Cvitlo.Auth.Application.Features.Account.UnlinkAccount;

/// <summary>Represents a request to unlink an identity provider.</summary>
/// <param name="Alias">The provider alias, such as google or github.</param>
public record UnlinkAccountRequest(string Alias);
