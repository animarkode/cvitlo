using System.Net;
using Cvitlo.Resumes.Application.Features.Experiences.CreateExperience;
using Cvitlo.Resumes.Application.Features.Experiences.GetExperience;
using Cvitlo.Resumes.Application.Features.Experiences.UpdateExperience;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Experiences.UpdateExperience.Data;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Experiences.UpdateExperience;

[Collection<CollectionResumes>]
[Priority(32)]
public class UpdateExperienceTests(WebApp app) : TestBase
{
    [Fact]
    [Priority(0)]
    public async Task UpdateExperience_NoToken_ShouldReturnUnauthorized()
    {
        var httpResponse = await app.Client
            .PUTAsync<UpdateExperienceEndpoint, UpdateExperienceRequest>(
                new UpdateExperienceRequest(Guid.NewGuid(), Guid.NewGuid(), "Title", "Employer", null, null, null, null));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Priority(1)]
    public async Task UpdateExperience_ShouldModifyFields()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = new CreateExperienceRequestFaker(resume.Id).Generate();

        var (_, ehResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateExperienceEndpoint, CreateExperienceRequest,
                CreateExperienceResponse>(createRequest);

        var updated = new CreateExperienceRequestFaker(resume.Id, seed: 32001).Generate();
        var updateRequest = new UpdateExperienceRequest(
            ehResponse.Id, resume.Id,
            updated.JobTitle, updated.Employer, updated.StartDate, updated.EndDate,
            updated.City, updated.Description);

        var updateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateExperienceEndpoint, UpdateExperienceRequest>(updateRequest);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (_, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetExperienceEndpoint, GetExperienceRequest, GetExperienceResponse>(
                new GetExperienceRequest(resume.Id, ehResponse.Id));

        getResponse.JobTitle.ShouldBe(updated.JobTitle);
        getResponse.Employer.ShouldBe(updated.Employer);
    }

    [Fact]
    [Priority(2)]
    public async Task UpdateExperience_UnknownResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var eh = new CreateExperienceRequestFaker(Guid.NewGuid()).Generate();
        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateExperienceEndpoint, UpdateExperienceRequest>(
                new UpdateExperienceRequest(Guid.NewGuid(), Guid.NewGuid(),
                    eh.JobTitle, eh.Employer, eh.StartDate, eh.EndDate,
                    eh.City, eh.Description));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Priority(3)]
    public async Task CrossUserUpdateExperience_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);

        var createRequest = new CreateExperienceRequestFaker(resume.Id).Generate();

        var (_, ehResponse) = await app.Client
            .WithAuthToken(userAToken)
            .POSTAsync<CreateExperienceEndpoint, CreateExperienceRequest,
                CreateExperienceResponse>(createRequest);

        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);

        var updated = new CreateExperienceRequestFaker(resume.Id, seed: 32002).Generate();
        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .PUTAsync<UpdateExperienceEndpoint, UpdateExperienceRequest>(
                new UpdateExperienceRequest(ehResponse.Id, resume.Id,
                    updated.JobTitle, updated.Employer, updated.StartDate, updated.EndDate,
                    updated.City, updated.Description));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [ClassData(typeof(InvalidUpdateExperienceData))]
    [Priority(4)]
    public async Task UpdateExperience_InvalidField_ShouldReturnBadRequest(
        UpdateExperienceRequest invalidRequest,
        string expectedKey)
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var created = await CreateExperienceAsync(accessToken, resume.Id);

        var updateRequest = invalidRequest with { Id = created.Id, ResumeId = resume.Id };

        var (httpResponse, error) = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateExperienceEndpoint, UpdateExperienceRequest, ErrorResponse>(updateRequest);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        error.Errors.Keys.ShouldBe([expectedKey]);
    }

    [Fact]
    [Priority(5)]
    public async Task UpdateExperience_WithUnsafeDescription_ShouldPersistSanitizedHtml()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = new CreateExperienceRequestFaker(resume.Id).Generate();

        var (_, ehResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateExperienceEndpoint, CreateExperienceRequest,
                CreateExperienceResponse>(createRequest);

        const string unsafeDescription =
            "<p><strong>Lead</strong> developer</p>"
            + "<script>alert('xss')</script>"
            + "<img src=x onerror=\"alert(1)\" />";

        var updateRequest = new UpdateExperienceRequest(
            ehResponse.Id, resume.Id,
            "Engineer", "Acme", null, null, "London", unsafeDescription);

        var updateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateExperienceEndpoint, UpdateExperienceRequest>(updateRequest);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (_, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetExperienceEndpoint, GetExperienceRequest, GetExperienceResponse>(
                new GetExperienceRequest(resume.Id, ehResponse.Id));

        getResponse.Description.ShouldNotBeNull();
        getResponse.Description.ShouldNotContain("<script");
        getResponse.Description.ShouldNotContain("onerror");
        getResponse.Description.ShouldContain("<strong>Lead</strong>");
    }

    [Fact]
    [Priority(6)]
    public async Task UpdateExperience_OwnResumeWithAnotherUsersItem_ShouldReturnNotFoundAndChangeNothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);
        var resumeA = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);
        var resumeB = await ResumesTestHelper.CreateResumeAsync(app.Client, userBToken, ct: ct);
        var item = await CreateExperienceAsync(userAToken, resumeA.Id);

        var source = new CreateExperienceRequestFaker(resumeB.Id, 62003).Generate() with { JobTitle = "intruder" };

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .PUTAsync<UpdateExperienceEndpoint, UpdateExperienceRequest>(new UpdateExperienceRequest(item.Id, resumeB.Id, source.JobTitle, source.Employer, source.StartDate, source.EndDate, source.City, source.Description));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var (_, after) = await app.Client
            .WithAuthToken(userAToken)
            .GETAsync<GetExperienceEndpoint, GetExperienceRequest, GetExperienceResponse>(
                new GetExperienceRequest(resumeA.Id, item.Id));

        after.JobTitle.ShouldBe(item.JobTitle);
    }

    [Fact]
    [Priority(7)]
    public async Task UpdateExperience_UnknownItem_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var source = new CreateExperienceRequestFaker(resume.Id).Generate();

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateExperienceEndpoint, UpdateExperienceRequest>(new UpdateExperienceRequest(Guid.NewGuid(), resume.Id, source.JobTitle, source.Employer, source.StartDate, source.EndDate, source.City, source.Description));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(Skip = "test-audit 05-B1: 1000 ampersands pass validation, grow to 5000 chars when sanitized and overflow varchar(1000); answers 500 instead of 200/400")]
    [Priority(8)]
    public async Task UpdateExperience_DescriptionThatGrowsWhenEncoded_ShouldNotFail()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var created = await CreateExperienceAsync(accessToken, resume.Id);

        var source = new CreateExperienceRequestFaker(resume.Id, 62004).Generate() with { Description = new string('&', 1000) };

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateExperienceEndpoint, UpdateExperienceRequest>(new UpdateExperienceRequest(created.Id, resume.Id, source.JobTitle, source.Employer, source.StartDate, source.EndDate, source.City, source.Description));

        httpResponse.StatusCode.ShouldBeOneOf(HttpStatusCode.NoContent, HttpStatusCode.BadRequest);
    }

    [Fact(Skip = "test-audit 05-B3: the update validator has no future StartDate rule (create has); answers 204")]
    [Priority(9)]
    public async Task UpdateExperience_FutureStartDate_ShouldReturnBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var created = await CreateExperienceAsync(accessToken, resume.Id);

        var source = new CreateExperienceRequestFaker(resume.Id, 62005).Generate() with
        {
            StartDate = new DateOnly(2099, 1, 1),
            EndDate = null
        };

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateExperienceEndpoint, UpdateExperienceRequest>(new UpdateExperienceRequest(created.Id, resume.Id, source.JobTitle, source.Employer, source.StartDate, source.EndDate, source.City, source.Description));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task<CreateExperienceResponse> CreateExperienceAsync(
        string accessToken,
        Guid resumeId,
        int seed = ResumesTestConstants.ExperienceSeed)
    {
        var (_, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateExperienceEndpoint, CreateExperienceRequest, CreateExperienceResponse>(
                new CreateExperienceRequestFaker(resumeId, seed).Generate());

        return response;
    }
}
