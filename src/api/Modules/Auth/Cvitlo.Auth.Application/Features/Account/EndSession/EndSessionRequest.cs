namespace Cvitlo.Auth.Application.Features.Account.EndSession;

/// <summary>Represents a request to end a device session.</summary>
/// <param name="Id">Keycloak's id of the session.</param>
public record EndSessionRequest(string Id);
