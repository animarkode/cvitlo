using System.Security.Claims;
using Cvitlo.Auth.Application.Sessions;
using Cvitlo.Auth.PublicApi;

namespace Cvitlo.Auth.Application.Features.User;

public static class UserMapper
{
    public static UserResponse MapToResponse(this ClaimsPrincipal user)
        => new(
            user.FindFirst(JwtClaimTypes.Aliases.UserId)!.Value,
            user.FindFirst(JwtClaimTypes.OidcStandard.Email)?.Value,
            user.FindFirst(JwtClaimTypes.OidcStandard.GivenName)?.Value,
            user.FindFirst(JwtClaimTypes.OidcStandard.FamilyName)?.Value,
            user.FindFirst(JwtClaimTypes.OidcStandard.Name)?.Value,
            SessionClaims.Roles(user));
}
