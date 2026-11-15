namespace Cvitlo.Documents.PublicApi.Resumes;

/// <summary>One spoken-language entry.</summary>
public record LanguageDocument
{
    public string? Name { get; init; }
    public string? Level { get; init; }
}
