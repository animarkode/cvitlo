namespace Cvitlo.Documents.PublicApi.Resumes;

/// <summary>One labelled link.</summary>
public record LinkDocument
{
    public string? Label { get; init; }
    public string? Uri { get; init; }
}
