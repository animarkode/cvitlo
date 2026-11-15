namespace Cvitlo.Documents.PublicApi.Resumes;

/// <summary>One project entry; Description is rendered as sanitized HTML.</summary>
public record ProjectDocument
{
    public string? Name { get; init; }
    public string? Tagline { get; init; }
    public string? Url { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? Description { get; init; }
}
