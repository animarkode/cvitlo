using System.Net;
using Cvitlo.Resumes.Application.Features.Experiences.CreateExperience;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Experiences.CreateExperience.Data;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;
using Cvitlo.Resumes.Application.Features.Experiences.GetExperience;
using Cvitlo.Resumes.Application.Features.Experiences.DeleteExperience;
using Cvitlo.Resumes.Application.Features.Experiences.GetAllExperiences;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Experiences.CreateExperience;

[Collection<CollectionResumes>]
[Priority(30)]
public class CreateExperienceTests(WebApp app) : TestBase
{
    [Theory]
    [ClassData(typeof(ValidExperienceData))]
    [Priority(1)]
    public async Task CreateExperience_ValidRequest_ShouldReturnTheCreatedItem(CreateExperienceRequest request)
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = request with { ResumeId = resume.Id };

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateExperienceEndpoint, CreateExperienceRequest,
                CreateExperienceResponse>(createRequest);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Id.ShouldNotBe(Guid.Empty);
        response.ResumeId.ShouldBe(resume.Id);
        response.JobTitle.ShouldBe(request.JobTitle);
        response.Employer.ShouldBe(request.Employer);
        response.StartDate.ShouldBe(request.StartDate);
        response.EndDate.ShouldBe(request.EndDate);
        response.City.ShouldBe(request.City);
        response.Description.ShouldBe(request.Description);
        response.Order.ShouldBe(0);
    }

    [Fact]
    [Priority(2)]
    public async Task CreateExperienceForNonExistentResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var request = new CreateExperienceRequestFaker(Guid.NewGuid()).Generate();

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateExperienceEndpoint, CreateExperienceRequest,
                CreateExperienceResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [ClassData(typeof(InvalidExperienceData))]
    [Priority(2)]
    public async Task CreateExperience_InvalidField_ShouldReturnBadRequest(
        CreateExperienceRequest invalidRequest,
        string expectedKey)
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = invalidRequest with { ResumeId = resume.Id };

        var (httpResponse, error) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateExperienceEndpoint, CreateExperienceRequest, ErrorResponse>(createRequest);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        error.Errors.Keys.ShouldBe([expectedKey]);
    }

    [Fact]
    [Priority(3)]
    public async Task CreateExperience_FieldsAtMaximumLength_ShouldReturnOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var request = new CreateExperienceRequestFaker(resume.Id).Generate() with { JobTitle = new string('a', 100), Employer = new string('a', 100), City = new string('a', 100), Description = new string('a', 1000) };

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateExperienceEndpoint, CreateExperienceRequest, CreateExperienceResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.JobTitle!.Length.ShouldBe(100);
        response.Employer!.Length.ShouldBe(100);
        response.City!.Length.ShouldBe(100);
        response.Description!.Length.ShouldBe(1000);
    }

    [Fact]
    [Priority(4)]
    public async Task CreateExperience_StartDateToday_ShouldReturnOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var request = new CreateExperienceRequestFaker(resume.Id).Generate() with { StartDate = today, EndDate = null };

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateExperienceEndpoint, CreateExperienceRequest, CreateExperienceResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.StartDate.ShouldBe(today);
    }

    [Fact]
    [Priority(5)]
    public async Task CreateExperience_UnsafeDescription_ShouldStoreSanitizedHtml()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        const string unsafeDescription =
            "<p><strong>Module</strong> on XSS</p>"
            + "<script>alert('xss')</script>"
            + "<img src=x onerror=\"alert(1)\" />";

        var request = new CreateExperienceRequestFaker(resume.Id).Generate() with { Description = unsafeDescription };

        var (createResponse, created) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateExperienceEndpoint, CreateExperienceRequest, CreateExperienceResponse>(request);

        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var (_, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetExperienceEndpoint, GetExperienceRequest, GetExperienceResponse>(
                new GetExperienceRequest(resume.Id, created.Id));

        getResponse.Description.ShouldNotBeNull();
        getResponse.Description.ShouldNotContain("<script");
        getResponse.Description.ShouldNotContain("onerror");
        getResponse.Description.ShouldContain("<strong>Module</strong>");
    }

    [Fact(Skip = "test-audit 05-B1: 1000 ampersands pass validation, grow to 5000 chars when sanitized and overflow varchar(1000); answers 500 instead of 200/400")]
    [Priority(6)]
    public async Task CreateExperience_DescriptionThatGrowsWhenEncoded_ShouldNotFail()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var request = new CreateExperienceRequestFaker(resume.Id).Generate() with { Description = new string('&', 1000) };

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateExperienceEndpoint, CreateExperienceRequest, ErrorResponse>(request);

        httpResponse.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    [Priority(7)]
    public async Task CreateExperience_SeveralItems_ShouldNumberThemInCreationOrder()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var first = await CreateExperienceAsync(accessToken, resume.Id, 65001);
        var second = await CreateExperienceAsync(accessToken, resume.Id, 65002);
        var third = await CreateExperienceAsync(accessToken, resume.Id, 65003);

        new[] { first.Order, second.Order, third.Order }.ShouldBe(new[] { 0, 1, 2 });

        var (_, all) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetAllExperiencesEndpoint, GetAllExperiencesRequest, GetAllExperiencesResponse>(
                new GetAllExperiencesRequest(resume.Id));

        all.Items.Select(i => i.Id).ShouldBe([first.Id, second.Id, third.Id]);
        all.Items.Select(i => i.Order).ShouldBe(new[] { 0, 1, 2 });
    }

    [Fact]
    [Priority(8)]
    public async Task CreateExperience_AfterDeletingTheMiddleItem_ShouldAppendAfterTheLast()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var first = await CreateExperienceAsync(accessToken, resume.Id, 65001);
        var middle = await CreateExperienceAsync(accessToken, resume.Id, 65002);
        var last = await CreateExperienceAsync(accessToken, resume.Id, 65003);

        var deleteResponse = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteExperienceEndpoint, DeleteExperienceRequest>(new DeleteExperienceRequest(resume.Id, middle.Id));

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var appended = await CreateExperienceAsync(accessToken, resume.Id, 65004);

        appended.Order.ShouldBe(3);

        var (_, all) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetAllExperiencesEndpoint, GetAllExperiencesRequest, GetAllExperiencesResponse>(
                new GetAllExperiencesRequest(resume.Id));

        all.Items.Select(i => i.Id).ShouldBe([first.Id, last.Id, appended.Id]);
        all.Items.Select(i => i.Order).ShouldBe(new[] { 0, 2, 3 });
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
