namespace Cvitlo.Auth.Application.Features.Account.GetCredentials;

/// <summary>Describes a credential type and the user's stored credentials of that type.</summary>
/// <param name="Type">Keycloak's credential type, e.g. <c>password</c>, <c>webauthn-passwordless</c>, <c>otp</c>.</param>
/// <param name="Category">The credential category.</param>
/// <param name="DisplayName">The display name of the credential type.</param>
/// <param name="HelpText">Help text for the credential type.</param>
/// <param name="CreateAction">App action name (see <c>AccountActions</c>) that adds one; null when none is available.</param>
/// <param name="UpdateAction">App action name that replaces it; null when none is available.</param>
/// <param name="Removeable">Whether stored credentials of this type can be removed.</param>
/// <param name="Credentials">What the user has stored of this type.</param>
public record GetCredentialsResponse(
    string Type,
    string? Category,
    string? DisplayName,
    string? HelpText,
    string? CreateAction,
    string? UpdateAction,
    bool Removeable,
    IReadOnlyList<StoredCredential> Credentials);
