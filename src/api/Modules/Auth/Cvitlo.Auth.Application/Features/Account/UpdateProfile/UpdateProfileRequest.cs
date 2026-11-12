namespace Cvitlo.Auth.Application.Features.Account.UpdateProfile;

/// <summary>Represents changes to the user's profile.</summary>
/// <param name="FirstName">Left unchanged when null.</param>
/// <param name="LastName">Left unchanged when null.</param>
/// <param name="Attributes">Other editable profile fields by Keycloak name; an empty value clears one.</param>
public record UpdateProfileRequest(string? FirstName, string? LastName, IReadOnlyDictionary<string, string?>? Attributes);
