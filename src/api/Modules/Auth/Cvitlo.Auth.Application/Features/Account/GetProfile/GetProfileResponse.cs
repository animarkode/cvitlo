namespace Cvitlo.Auth.Application.Features.Account.GetProfile;

public record GetProfileResponse(
    string? Username,
    string? Email,
    bool EmailVerified,
    string? FirstName,
    string? LastName,
    IReadOnlyList<ProfileField> Fields);
