using System.Net;
using Cvitlo.Resumes.Application.Features.Experiences.CreateExperience;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Experiences.CreateExperience.Data;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

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
}
