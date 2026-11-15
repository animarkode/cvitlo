namespace Cvitlo.Auth.Application.Sessions;

// CreatedDate is epoch milliseconds.
public sealed record KeycloakCredential(string Id, string? Type, string? UserLabel, long? CreatedDate);
