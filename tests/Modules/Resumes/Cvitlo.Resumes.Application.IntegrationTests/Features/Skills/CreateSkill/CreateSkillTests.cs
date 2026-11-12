using System.Net;
using Cvitlo.Resumes.Application.Features.Skills.CreateSkill;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Skills.CreateSkill.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Skills.CreateSkill;

[Collection<CollectionResumes>]
[Priority(40)]
public class CreateSkillTests(WebApp app) : TestBase
{
    [Theory]
    [ClassData(typeof(ValidSkillData))]
    [Priority(1)]
    public async Task CreateSkill_ShouldReturnCreated(CreateSkillRequest request)
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = request with { ResumeId = resume.Id };

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateSkillEndpoint, CreateSkillRequest, CreateSkillResponse>(createRequest);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Id.ShouldNotBe(Guid.Empty);
        response.ResumeId.ShouldBe(resume.Id);
        response.Name.ShouldBe(request.Name);
        response.Level.ShouldBe(request.Level);
    }

    [Fact]
    [Priority(2)]
    public async Task CreateSkillForNonExistentResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var request = new CreateSkillRequestFaker(Guid.NewGuid()).Generate();

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateSkillEndpoint, CreateSkillRequest, CreateSkillResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [ClassData(typeof(InvalidSkillData))]
    [Priority(2)]
    public async Task CreateSkill_WithInvalidData_ShouldReturnBadRequest(
        CreateSkillRequest invalidRequest)
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        
        var createRequest = invalidRequest with { ResumeId = resume.Id };
        
        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateSkillEndpoint, CreateSkillRequest, CreateSkillResponse>(createRequest);
        
        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
