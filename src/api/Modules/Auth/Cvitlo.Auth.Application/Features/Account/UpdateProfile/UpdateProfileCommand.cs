using ErrorOr;
using Cvitlo.Auth.Application.Features.Account.GetProfile;
using FastEndpoints;

namespace Cvitlo.Auth.Application.Features.Account.UpdateProfile;

public record UpdateProfileCommand(
    string AccessToken,
    string? FirstName,
    string? LastName,
    IReadOnlyDictionary<string, string?> Attributes) : ICommand<ErrorOr<GetProfileResponse>>;
