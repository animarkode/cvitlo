using System.Net;
using Cvitlo.Resumes.Application.Features.Resumes.CreateResume;
using Cvitlo.Resumes.Application.Features.Resumes.GetResume;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Resumes.GetResume;

[Collection<CollectionResumes>]
public class GetResumeTests(WebApp app) : TestBase
{
    [Fact]
    public async Task GetResume_NoToken_ShouldReturnUnauthorized()
    {
        var (httpResponse, _) = await app.Client
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(
                new GetResumeRequest(Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetResume_ExistingResume_ShouldReturnResume()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var createRequest = new CreateResumeRequest("en-us");

        var (_, createResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateResumeEndpoint, CreateResumeRequest, CreateResumeResponse>(createRequest);

        var getRequest = new GetResumeRequest(createResponse.Id);

        var (httpResponse, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(getRequest);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        getResponse.Id.ShouldBe(createResponse.Id);
        getResponse.Localization.ShouldBe("en-us");
    }

    [Fact]
    public async Task GetResume_UnknownResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var getRequest = new GetResumeRequest(Guid.NewGuid());

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(getRequest);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetResume_OtherUsersResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);

        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(userBToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(
                new GetResumeRequest(resume.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetResume_ResumeWithEverySection_ShouldReturnAllSections()
    {
        var ct = TestContext.Current.CancellationToken;
        var token = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var seeded = await ResumeWithEverySectionSeeder.SeedAsync(app, token, ct);

        var (httpResponse, resume) = await app.Client
            .WithAuthToken(token)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(new GetResumeRequest(seeded.ResumeId));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        resume.Person.ShouldNotBeNull().Id.ShouldBe(seeded.PersonId);
        resume.Experience.Select(x => x.Id).ShouldBe([seeded.ExperienceId]);
        resume.Educations.Select(x => x.Id).ShouldBe([seeded.EducationId]);
        resume.Skills.Select(x => x.Id).ShouldBe([seeded.SkillId]);
        resume.Languages.Select(x => x.Id).ShouldBe([seeded.LanguageId]);
        resume.Courses.Select(x => x.Id).ShouldBe([seeded.CourseId]);
        resume.Projects.Select(x => x.Id).ShouldBe([seeded.ProjectId]);
        resume.Links.Select(x => x.Id).ShouldBe([seeded.LinkId]);
    }
}
