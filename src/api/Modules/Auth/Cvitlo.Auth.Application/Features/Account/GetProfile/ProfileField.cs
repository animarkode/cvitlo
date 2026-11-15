namespace Cvitlo.Auth.Application.Features.Account.GetProfile;

public record ProfileField(
    string Name,
    string? DisplayName,
    bool Required,
    bool ReadOnly,
    int? MinLength,
    int? MaxLength,
    string? Value);
