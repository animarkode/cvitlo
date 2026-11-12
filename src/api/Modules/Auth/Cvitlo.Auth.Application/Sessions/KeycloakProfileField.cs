using System.Text.Json;

namespace Cvitlo.Auth.Application.Sessions;

/// <summary>Describes a profile field configured in Keycloak.</summary>
/// <param name="Name">The attribute name.</param>
/// <param name="DisplayName">The field's display name or message key.</param>
/// <param name="Required">Whether a value is required.</param>
/// <param name="ReadOnly">Whether the field is read-only.</param>
/// <param name="Multivalued">Whether the field accepts multiple values.</param>
/// <param name="Validators">Keycloak's validators by name; each value is that validator's own configuration.</param>
public sealed record KeycloakProfileField(
    string Name,
    string? DisplayName,
    bool Required,
    bool ReadOnly,
    bool Multivalued,
    IReadOnlyDictionary<string, JsonElement>? Validators);
