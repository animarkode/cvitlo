namespace Faber.Modules.Auth.Application.Features.Account.GetProfile;

/// <summary>Describes the user's profile.</summary>
/// <param name="Username">The user's username.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="EmailVerified">Whether the email address is verified.</param>
/// <param name="FirstName">The user's first name.</param>
/// <param name="LastName">The user's last name.</param>
/// <param name="Fields">The fields of the realm's user profile and the user's value for each.</param>
public record GetProfileResponse(
    string? Username,
    string? Email,
    bool EmailVerified,
    string? FirstName,
    string? LastName,
    IReadOnlyList<ProfileField> Fields);
