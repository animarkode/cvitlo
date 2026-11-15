using System.Text.Json.Serialization;

namespace Cvitlo.Auth.Application.Sessions;

public sealed record KeycloakAccount(
    string? Username,
    string? Email,
    bool EmailVerified,
    string? FirstName,
    string? LastName,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? Attributes,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    KeycloakUserProfileMetadata? UserProfileMetadata);
