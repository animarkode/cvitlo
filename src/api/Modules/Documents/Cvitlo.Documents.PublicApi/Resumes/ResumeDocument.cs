namespace Cvitlo.Documents.PublicApi.Resumes;

/// <summary>The resume data a template renders; Documents does not depend on the Resumes domain.</summary>
public record ResumeDocument
{
    public string? Localization { get; init; }
    public string? Summary { get; init; }
    public string? Hobbies { get; init; }
    public PersonDocument? Person { get; init; }
    public IReadOnlyList<ExperienceDocument> Experiences { get; init; } = [];
    public IReadOnlyList<EducationDocument> Educations { get; init; } = [];
    public IReadOnlyList<LinkDocument> Links { get; init; } = [];
    public IReadOnlyList<SkillDocument> Skills { get; init; } = [];
    public IReadOnlyList<LanguageDocument> Languages { get; init; } = [];
    public IReadOnlyList<CourseDocument> Courses { get; init; } = [];
    public IReadOnlyList<ProjectDocument> Projects { get; init; } = [];
}
