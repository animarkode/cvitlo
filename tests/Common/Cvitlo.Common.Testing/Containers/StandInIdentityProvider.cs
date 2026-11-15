namespace Cvitlo.Common.Testing.Containers;

/// <summary>
/// Describes a realm that stands in for an external identity provider in a throwaway Keycloak container.
/// Its only user has a verified e-mail.
/// </summary>
public sealed record StandInIdentityProvider(
    string Alias,
    string Realm,
    string ClientId,
    string ClientSecret,
    string Username,
    string Password,
    string Email,
    string FirstName,
    string LastName);
