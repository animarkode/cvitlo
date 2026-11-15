using System.Text.Json;

namespace Cvitlo.Auth.Application.Sessions;

public sealed record KeycloakProfileField(
    string Name,
    string? DisplayName,
    bool Required,
    bool ReadOnly,
    bool Multivalued,
    IReadOnlyDictionary<string, JsonElement>? Validators);
