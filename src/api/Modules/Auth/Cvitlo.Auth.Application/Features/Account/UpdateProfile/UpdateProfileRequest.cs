namespace Cvitlo.Auth.Application.Features.Account.UpdateProfile;

// FirstName and LastName are left unchanged when null; an empty attribute value clears that attribute.
public record UpdateProfileRequest(string? FirstName, string? LastName, IReadOnlyDictionary<string, string?>? Attributes);
