namespace Cvitlo.Auth.Application.Sessions;

/// <summary>Describes a credential stored by Keycloak.</summary>
/// <param name="Id">The credential identifier.</param>
/// <param name="Type">The credential type.</param>
/// <param name="UserLabel">The user-provided label.</param>
/// <param name="CreatedDate">Epoch milliseconds.</param>
public sealed record KeycloakCredential(string Id, string? Type, string? UserLabel, long? CreatedDate);
