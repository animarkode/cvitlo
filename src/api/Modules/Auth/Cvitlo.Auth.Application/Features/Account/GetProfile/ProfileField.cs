namespace Cvitlo.Auth.Application.Features.Account.GetProfile;

/// <summary>Describes a user profile field and its value.</summary>
/// <param name="Name">Keycloak's attribute name, the key to send back when updating.</param>
/// <param name="DisplayName">Keycloak's label or message key, e.g. <c>${firstName}</c>.</param>
/// <param name="Required">Whether the field requires a value.</param>
/// <param name="ReadOnly">True for fields the user cannot change here; the e-mail always is.</param>
/// <param name="MinLength">The minimum permitted value length, if configured.</param>
/// <param name="MaxLength">The maximum permitted value length, if configured.</param>
/// <param name="Value">The user's current value.</param>
public record ProfileField(
    string Name,
    string? DisplayName,
    bool Required,
    bool ReadOnly,
    int? MinLength,
    int? MaxLength,
    string? Value);
