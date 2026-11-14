using System.Net;
using Cvitlo.Resumes.Application.Features.Resumes.CreateResume;
using Cvitlo.Resumes.Application.Features.Resumes.GetResume;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Application.Features.Resumes.UpdateLocalization;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Resumes.UpdateLocalization;

[Collection<CollectionResumes>]
[Priority(7)]
public class UpdateLocalizationTests(WebApp app) : TestBase
{
    [Fact]
    [Priority(0)]
    public async Task UpdateLocalization_NoToken_ShouldReturnUnauthorized()
    {
        var httpResponse = await app.Client
            .PUTAsync<UpdateLocalizationEndpoint, UpdateLocalizationRequest>(
                new UpdateLocalizationRequest(Guid.NewGuid(), "en-us"));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Priority(1)]
    public async Task UpdateLocalization_ShouldModifyLocalization()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var createRequest = new CreateResumeRequest("en-us");

        var (_, createResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateResumeEndpoint, CreateResumeRequest, CreateResumeResponse>(createRequest);

        var updateRequest = new UpdateLocalizationRequest(createResponse.Id, "uk-ua");

        var updateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateLocalizationEndpoint, UpdateLocalizationRequest>(updateRequest);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getRequest = new GetResumeRequest(createResponse.Id);

        var (_, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(getRequest);

        getResponse.Localization.ShouldBe("uk-ua");
    }

    [Fact]
    [Priority(2)]
    public async Task NonExistentResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateLocalizationEndpoint, UpdateLocalizationRequest>(
                new UpdateLocalizationRequest(Guid.NewGuid(), "uk-ua"));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateLocalization_OtherUsersResume_ShouldReturnForbiddenAndChangeNothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var ownerToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var intruderToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, ownerToken, ct: ct);

        var ownerUpdate = await app.Client
            .WithAuthToken(ownerToken)
            .PUTAsync<UpdateLocalizationEndpoint, UpdateLocalizationRequest>(new UpdateLocalizationRequest(resume.Id, "uk-ua"));
        ownerUpdate.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var intruderUpdate = await app.Client
            .WithAuthToken(intruderToken)
            .PUTAsync<UpdateLocalizationEndpoint, UpdateLocalizationRequest>(new UpdateLocalizationRequest(resume.Id, "de-de"));
        intruderUpdate.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var (_, stored) = await app.Client
            .WithAuthToken(ownerToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(new GetResumeRequest(resume.Id));
        stored.Localization.ShouldBe("uk-ua");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateLocalization_EmptyValue_ShouldReturnBadRequest(string value)
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateLocalizationEndpoint, UpdateLocalizationRequest, ErrorResponse>(
                new UpdateLocalizationRequest(resume.Id, value));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Errors.ShouldContainKey("localization");
    }

    [Fact]
    public async Task UpdateLocalization_101Characters_ShouldReturnBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateLocalizationEndpoint, UpdateLocalizationRequest, ErrorResponse>(
                new UpdateLocalizationRequest(resume.Id, new string('a', 101)));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Errors.ShouldContainKey("localization");
    }

    [Fact]
    public async Task UpdateLocalization_100Characters_ShouldReturnNoContent()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateLocalizationEndpoint, UpdateLocalizationRequest>(
                new UpdateLocalizationRequest(resume.Id, new string('a', 100)));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
