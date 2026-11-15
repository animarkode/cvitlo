namespace Cvitlo.Auth.Application.Features.Account.GetCredentials;

public record GetCredentialsResponse(
    string Type,
    string? Category,
    string? DisplayName,
    string? HelpText,
    string? CreateAction,
    string? UpdateAction,
    bool Removeable,
    IReadOnlyList<StoredCredential> Credentials);
