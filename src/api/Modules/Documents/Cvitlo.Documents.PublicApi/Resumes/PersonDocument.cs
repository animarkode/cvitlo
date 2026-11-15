namespace Cvitlo.Documents.PublicApi.Resumes;

/// <summary>The personal details and contacts shown in a resume header.</summary>
public record PersonDocument
{
    public string? JobTitle { get; init; }
    public string? Firstname { get; init; }
    public string? Lastname { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Country { get; init; }
    public string? City { get; init; }
    public string? Street { get; init; }
    public string? PostCode { get; init; }
    public string? Nationality { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? DrivingLicense { get; init; }
}
