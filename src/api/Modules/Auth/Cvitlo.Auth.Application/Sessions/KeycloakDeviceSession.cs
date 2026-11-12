namespace Cvitlo.Auth.Application.Sessions;

/// <summary>Describes a device session managed by Keycloak.</summary>
/// <param name="Id">The session identifier.</param>
/// <param name="IpAddress">The device's IP address.</param>
/// <param name="Started">Epoch seconds.</param>
/// <param name="LastAccess">Epoch seconds.</param>
/// <param name="Expires">Epoch seconds.</param>
/// <param name="Browser">The device's browser.</param>
/// <param name="Current">Whether this is the current session.</param>
public sealed record KeycloakDeviceSession(
    string Id,
    string? IpAddress,
    long Started,
    long LastAccess,
    long Expires,
    string? Browser,
    bool Current);
