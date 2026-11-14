using Cvitlo.Resumes.Application.Features.Courses.CreateCourse;
using Cvitlo.Resumes.Application.Features.Educations.CreateEducation;
using Cvitlo.Resumes.Application.Features.Experiences.CreateExperience;
using Cvitlo.Resumes.Application.Features.Languages.CreateLanguage;
using Cvitlo.Resumes.Application.Features.Links.CreateLink;
using Cvitlo.Resumes.Application.Features.Persons.CreatePerson;
using Cvitlo.Resumes.Application.Features.Projects.CreateProject;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Resumes.Application.Features.Skills.CreateSkill;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Resumes;

internal static class ResumeWithEverySectionSeeder
{
    public static async Task<SeededResume> SeedAsync(WebApp app, string token, CancellationToken ct)
    {
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, token, ct: ct);
        using var client = app.Client.WithAuthToken(token);
        var id = resume.Id;

        var (_, person) = await client
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(new CreatePersonRequestFaker(id).Generate());
        var (_, experience) = await client
            .POSTAsync<CreateExperienceEndpoint, CreateExperienceRequest, CreateExperienceResponse>(new CreateExperienceRequestFaker(id).Generate());
        var (_, education) = await client
            .POSTAsync<CreateEducationEndpoint, CreateEducationRequest, CreateEducationResponse>(new CreateEducationRequestFaker(id).Generate());
        var (_, skill) = await client
            .POSTAsync<CreateSkillEndpoint, CreateSkillRequest, CreateSkillResponse>(new CreateSkillRequestFaker(id).Generate());
        var (_, language) = await client
            .POSTAsync<CreateLanguageEndpoint, CreateLanguageRequest, CreateLanguageResponse>(new CreateLanguageRequestFaker(id).Generate());
        var (_, course) = await client
            .POSTAsync<CreateCourseEndpoint, CreateCourseRequest, CreateCourseResponse>(new CreateCourseRequestFaker(id).Generate());
        var (_, project) = await client
            .POSTAsync<CreateProjectEndpoint, CreateProjectRequest, CreateProjectResponse>(new CreateProjectRequestFaker(id).Generate());
        var (_, link) = await client
            .POSTAsync<CreateLinkEndpoint, CreateLinkRequest, CreateLinkResponse>(new CreateLinkRequestFaker(id).Generate());

        return new SeededResume(id, person.Id, experience.Id, education.Id, skill.Id, language.Id, course.Id, project.Id, link.Id);
    }
}
