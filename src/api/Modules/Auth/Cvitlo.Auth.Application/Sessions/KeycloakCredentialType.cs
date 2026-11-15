using System.Text.Json.Serialization;

namespace Cvitlo.Auth.Application.Sessions;

public sealed record KeycloakCredentialType(
    string Type,
    string? Category,
    string? DisplayName,
    [property: JsonPropertyName("helptext")] string? HelpText,
    string? CreateAction,
    string? UpdateAction,
    bool Removeable,
    IReadOnlyList<KeycloakCredentialContainer>? UserCredentialMetadatas);
