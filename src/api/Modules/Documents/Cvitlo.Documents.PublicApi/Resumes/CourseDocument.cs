namespace Cvitlo.Documents.PublicApi.Resumes;

/// <summary>One course entry; Description is rendered as HTML.</summary>
public record CourseDocument
{
    public string? School { get; init; }
    public string? Name { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? Description { get; init; }
}
