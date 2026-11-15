namespace Cvitlo.ArchitectureTests;

internal static class KnownViolations
{
    private const string FirstTemplate = "Cvitlo.Documents.Application.Templates.FirstTemplate";
    private const string ResumeEntities = "Cvitlo.Resumes.Domain.Entities.";

    // Frozen: FirstTemplate.razor binds Resumes.Domain entities and ResumePdfCache renders it.
    // Exact types only; removed by the follow-up to #TBD.
    public static readonly IReadOnlyList<KnownViolation> ModuleBoundary =
    [
        new("Cvitlo.Resumes.Application.Features.Documents.Shared.ResumePdfCache", FirstTemplate),
        .. new[] { "Course", "Education", "Experience", "Language", "Link", "Person", "Project", "Resume", "Skill" }
            .Select(entity => new KnownViolation(FirstTemplate, ResumeEntities + entity)),
    ];
}
