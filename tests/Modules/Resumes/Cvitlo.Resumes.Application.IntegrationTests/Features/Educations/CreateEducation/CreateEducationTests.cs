using System.Net;
using Cvitlo.Resumes.Application.Features.Educations.CreateEducation;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Educations.CreateEducation.Data;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Educations.CreateEducation;

[Collection<CollectionResumes>]
[Priority(20)]
public class CreateEducationTests(WebApp app) : TestBase
{
    [Theory]
    [ClassData(typeof(ValidEducationData))]
    [Priority(1)]
    public async Task CreateEducation_ValidRequest_ShouldReturnTheCreatedItem(CreateEducationRequest request)
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = request with { ResumeId = resume.Id };

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateEducationEndpoint, CreateEducationRequest, CreateEducationResponse>(createRequest);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Id.ShouldNotBe(Guid.Empty);
        response.ResumeId.ShouldBe(resume.Id);
        response.School.ShouldBe(request.School);
        response.Degree.ShouldBe(request.Degree);
        response.StartDate.ShouldBe(request.StartDate);
        response.EndDate.ShouldBe(request.EndDate);
        response.City.ShouldBe(request.City);
        response.Description.ShouldBe(request.Description);
        response.Order.ShouldBe(0);
    }

    [Theory]
    [ClassData(typeof(InvalidEducationData))]
    [Priority(2)]
    public async Task CreateEducation_InvalidField_ShouldReturnBadRequest(
        CreateEducationRequest invalidRequest,
        string expectedKey)
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = invalidRequest with { ResumeId = resume.Id };

        var (httpResponse, error) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateEducationEndpoint, CreateEducationRequest, ErrorResponse>(createRequest);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        error.Errors.Keys.ShouldBe([expectedKey]);
    }

    [Fact]
    [Priority(2)]
    public async Task CreateEducationForNonExistentResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var request = new CreateEducationRequestFaker(Guid.NewGuid()).Generate();

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateEducationEndpoint, CreateEducationRequest, CreateEducationResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Priority(3)]
    public async Task CreateEducation_FieldsAtMaximumLength_ShouldReturnOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var request = new CreateEducationRequestFaker(resume.Id).Generate() with { School = new string('a', 100), Degree = new string('a', 100), City = new string('a', 100), Description = new string('a', 1000) };

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateEducationEndpoint, CreateEducationRequest, CreateEducationResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.School!.Length.ShouldBe(100);
        response.Degree!.Length.ShouldBe(100);
        response.City!.Length.ShouldBe(100);
        response.Description!.Length.ShouldBe(1000);
    }

    [Fact]
    [Priority(4)]
    public async Task CreateEducation_StartDateToday_ShouldReturnOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var request = new CreateEducationRequestFaker(resume.Id).Generate() with { StartDate = today, EndDate = null };

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateEducationEndpoint, CreateEducationRequest, CreateEducationResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.StartDate.ShouldBe(today);
    }
}
