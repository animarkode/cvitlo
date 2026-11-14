namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Resumes;

internal sealed record SeededResume(
    Guid ResumeId,
    Guid PersonId,
    Guid ExperienceId,
    Guid EducationId,
    Guid SkillId,
    Guid LanguageId,
    Guid CourseId,
    Guid ProjectId,
    Guid LinkId);
