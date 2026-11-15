namespace Cvitlo.Auth.Application.Features.Account.GetDevices;

public record GetDevicesResponse(
    string Id,
    string? Device,
    string? Os,
    string? OsVersion,
    string? Browser,
    string? IpAddress,
    DateTimeOffset Started,
    DateTimeOffset LastAccess,
    DateTimeOffset Expires,
    bool Current,
    bool Mobile);
