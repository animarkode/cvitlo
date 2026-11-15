namespace Cvitlo.ArchitectureTests;

internal static class KnownViolations
{
    private const string FirstTemplate = "Cvitlo.Documents.Application.Templates.FirstTemplate";
    private const string ResumeEntities = "Cvitlo.Resumes.Domain.Entities.";

    // Frozen: FirstTemplate.razor binds Resumes.Domain entities and ResumePdfCache renders it.
    // Exact types and project edges only; both lists are removed by #165.
    public static readonly IReadOnlyList<KnownViolation> ModuleBoundary =
    [
        new("Cvitlo.Resumes.Application.Features.Documents.Shared.ResumePdfCache", FirstTemplate),
        .. new[] { "Course", "Education", "Experience", "Language", "Link", "Person", "Project", "Resume", "Skill" }
            .Select(entity => new KnownViolation(FirstTemplate, ResumeEntities + entity)),
    ];

    public static readonly IReadOnlyList<KnownProjectReference> ProjectReferences =
    [
        new("Cvitlo.Resumes.Application", "Cvitlo.Documents.Application"),
        new("Cvitlo.Documents.Application", "Cvitlo.Resumes.Domain"),
    ];
}
