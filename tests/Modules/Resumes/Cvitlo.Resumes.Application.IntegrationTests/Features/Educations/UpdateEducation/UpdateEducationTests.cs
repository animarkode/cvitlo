using System.Net;
using Cvitlo.Resumes.Application.Features.Educations.CreateEducation;
using Cvitlo.Resumes.Application.Features.Educations.GetEducation;
using Cvitlo.Resumes.Application.Features.Educations.UpdateEducation;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Educations.UpdateEducation.Data;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Educations.UpdateEducation;

[Collection<CollectionResumes>]
[Priority(22)]
public class UpdateEducationTests(WebApp app) : TestBase
{
    [Fact]
    [Priority(0)]
    public async Task UpdateEducation_NoToken_ShouldReturnUnauthorized()
    {
        app.Client.DefaultRequestHeaders.Authorization = null;

        var httpResponse = await app.Client
            .PUTAsync<UpdateEducationEndpoint, UpdateEducationRequest>(
                new UpdateEducationRequest(Guid.NewGuid(), Guid.NewGuid(), "School", "Degree", null, null, null, null));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Priority(1)]
    public async Task UpdateEducation_ShouldModifyFields()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = new CreateEducationRequestFaker(resume.Id).Generate();

        var (_, educationResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateEducationEndpoint, CreateEducationRequest, CreateEducationResponse>(createRequest);

        var updated = new CreateEducationRequestFaker(resume.Id, seed: 22001).Generate();
        var updateRequest = new UpdateEducationRequest(
            educationResponse.Id, resume.Id,
            updated.School, updated.Degree, updated.StartDate, updated.EndDate,
            updated.City, updated.Description);

        var updateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateEducationEndpoint, UpdateEducationRequest>(updateRequest);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (_, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetEducationEndpoint, GetEducationRequest, GetEducationResponse>(
                new GetEducationRequest(resume.Id, educationResponse.Id));

        getResponse.School.ShouldBe(updated.School);
        getResponse.Degree.ShouldBe(updated.Degree);
    }

    [Fact]
    [Priority(2)]
    public async Task NonExistentEducation_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var edu = new CreateEducationRequestFaker(resume.Id).Generate();
        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateEducationEndpoint, UpdateEducationRequest>(
                new UpdateEducationRequest(Guid.NewGuid(), resume.Id, edu.School, edu.Degree,
                    edu.StartDate, edu.EndDate, edu.City, edu.Description));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Priority(3)]
    public async Task CrossUserUpdateEducation_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);

        var createRequest = new CreateEducationRequestFaker(resume.Id).Generate();

        var (_, educationResponse) = await app.Client
            .WithAuthToken(userAToken)
            .POSTAsync<CreateEducationEndpoint, CreateEducationRequest, CreateEducationResponse>(createRequest);

        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);

        var updated = new CreateEducationRequestFaker(resume.Id, seed: 22002).Generate();
        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .PUTAsync<UpdateEducationEndpoint, UpdateEducationRequest>(
                new UpdateEducationRequest(educationResponse.Id, resume.Id,
                    updated.School, updated.Degree, updated.StartDate, updated.EndDate,
                    updated.City, updated.Description));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [ClassData(typeof(InvalidUpdateEducationData))]
    [Priority(4)]
    public async Task UpdateEducation_InvalidField_ShouldReturnBadRequest(
        UpdateEducationRequest invalidRequest,
        string expectedKey)
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var created = await CreateEducationAsync(accessToken, resume.Id);

        var updateRequest = invalidRequest with { Id = created.Id, ResumeId = resume.Id };

        var (httpResponse, error) = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateEducationEndpoint, UpdateEducationRequest, ErrorResponse>(updateRequest);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        error.Errors.Keys.ShouldBe([expectedKey]);
    }

    [Fact]
    [Priority(5)]
    public async Task UpdateEducation_WithUnsafeDescription_ShouldPersistSanitizedHtml()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = new CreateEducationRequestFaker(resume.Id).Generate();

        var (_, educationResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateEducationEndpoint, CreateEducationRequest, CreateEducationResponse>(createRequest);

        const string unsafeDescription =
            "<p><strong>Thesis</strong> on security</p>"
            + "<script>alert('xss')</script>"
            + "<img src=x onerror=\"alert(1)\" />";

        var updateRequest = new UpdateEducationRequest(
            educationResponse.Id, resume.Id,
            "MIT", "MSc", null, null, "Boston", unsafeDescription);

        var updateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateEducationEndpoint, UpdateEducationRequest>(updateRequest);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (_, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetEducationEndpoint, GetEducationRequest, GetEducationResponse>(
                new GetEducationRequest(resume.Id, educationResponse.Id));

        getResponse.Description.ShouldNotBeNull();
        getResponse.Description.ShouldNotContain("<script");
        getResponse.Description.ShouldNotContain("onerror");
        getResponse.Description.ShouldContain("<strong>Thesis</strong>");
    }

    [Fact]
    [Priority(6)]
    public async Task UpdateEducation_OwnResumeWithAnotherUsersItem_ShouldReturnNotFoundAndChangeNothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);
        var resumeA = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);
        var resumeB = await ResumesTestHelper.CreateResumeAsync(app.Client, userBToken, ct: ct);
        var item = await CreateEducationAsync(userAToken, resumeA.Id);

        var source = new CreateEducationRequestFaker(resumeB.Id, 62003).Generate() with { School = "intruder" };

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .PUTAsync<UpdateEducationEndpoint, UpdateEducationRequest>(new UpdateEducationRequest(item.Id, resumeB.Id, source.School, source.Degree, source.StartDate, source.EndDate, source.City, source.Description));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var (_, after) = await app.Client
            .WithAuthToken(userAToken)
            .GETAsync<GetEducationEndpoint, GetEducationRequest, GetEducationResponse>(
                new GetEducationRequest(resumeA.Id, item.Id));

        after.School.ShouldBe(item.School);
    }

    [Fact(Skip = "test-audit 05-B1: 1000 ampersands pass validation, grow to 5000 chars when sanitized and overflow varchar(1000); answers 500 instead of 200/400")]
    [Priority(7)]
    public async Task UpdateEducation_DescriptionThatGrowsWhenEncoded_ShouldNotFail()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var created = await CreateEducationAsync(accessToken, resume.Id);

        var source = new CreateEducationRequestFaker(resume.Id, 62004).Generate() with { Description = new string('&', 1000) };

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateEducationEndpoint, UpdateEducationRequest>(new UpdateEducationRequest(created.Id, resume.Id, source.School, source.Degree, source.StartDate, source.EndDate, source.City, source.Description));

        httpResponse.StatusCode.ShouldBeOneOf(HttpStatusCode.NoContent, HttpStatusCode.BadRequest);
    }

    [Fact(Skip = "test-audit 05-B3: the update validator has no future StartDate rule (create has); answers 204")]
    [Priority(8)]
    public async Task UpdateEducation_FutureStartDate_ShouldReturnBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var created = await CreateEducationAsync(accessToken, resume.Id);

        var source = new CreateEducationRequestFaker(resume.Id, 62005).Generate() with
        {
            StartDate = new DateOnly(2099, 1, 1),
            EndDate = null
        };

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateEducationEndpoint, UpdateEducationRequest>(new UpdateEducationRequest(created.Id, resume.Id, source.School, source.Degree, source.StartDate, source.EndDate, source.City, source.Description));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task<CreateEducationResponse> CreateEducationAsync(
        string accessToken,
        Guid resumeId,
        int seed = ResumesTestConstants.EducationSeed)
    {
        var (_, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateEducationEndpoint, CreateEducationRequest, CreateEducationResponse>(
                new CreateEducationRequestFaker(resumeId, seed).Generate());

        return response;
    }
}
