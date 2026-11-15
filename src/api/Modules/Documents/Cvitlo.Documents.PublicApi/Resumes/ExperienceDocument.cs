namespace Cvitlo.Documents.PublicApi.Resumes;

/// <summary>One employment entry; Description is rendered as HTML.</summary>
public record ExperienceDocument
{
    public string? JobTitle { get; init; }
    public string? Employer { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? City { get; init; }
    public string? Description { get; init; }
}
