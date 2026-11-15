namespace Cvitlo.Documents.PublicApi.Resumes;

/// <summary>One education entry; Description is rendered as HTML.</summary>
public record EducationDocument
{
    public string? School { get; init; }
    public string? Degree { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? City { get; init; }
    public string? Description { get; init; }
}
