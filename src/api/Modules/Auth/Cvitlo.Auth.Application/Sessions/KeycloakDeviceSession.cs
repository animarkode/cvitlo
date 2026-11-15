namespace Cvitlo.Auth.Application.Sessions;

// Started, LastAccess and Expires are epoch seconds.
public sealed record KeycloakDeviceSession(
    string Id,
    string? IpAddress,
    long Started,
    long LastAccess,
    long Expires,
    string? Browser,
    bool Current);
