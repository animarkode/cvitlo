namespace Faber.Modules.Auth.Application.Features.Account.DeleteCredential;

/// <summary>Represents a request to delete a stored credential.</summary>
/// <param name="Id">Keycloak's id of the stored credential.</param>
public record DeleteCredentialRequest(string Id);
