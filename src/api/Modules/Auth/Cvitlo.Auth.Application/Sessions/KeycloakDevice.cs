namespace Cvitlo.Auth.Application.Sessions;

public sealed record KeycloakDevice(
    string? Device,
    string? Os,
    string? OsVersion,
    string? Browser,
    string? IpAddress,
    bool Mobile,
    bool Current,
    IReadOnlyList<KeycloakDeviceSession>? Sessions);
