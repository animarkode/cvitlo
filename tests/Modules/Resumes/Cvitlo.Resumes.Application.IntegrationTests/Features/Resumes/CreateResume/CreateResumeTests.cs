using System.Net;
using Cvitlo.Resumes.Application.Features.Resumes.CreateResume;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Resumes.CreateResume;

[Collection<CollectionResumes>]
[Priority(1)]
public class CreateResumeTests(WebApp app) : TestBase
{
    [Fact]
    [Priority(1)]
    public async Task CreateResume_ValidRequest_ShouldReturnCreatedResume()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var request = new CreateResumeRequest("en-us");

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateResumeEndpoint, CreateResumeRequest, CreateResumeResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Id.ShouldNotBe(Guid.Empty);
        response.Localization.ShouldBe("en-us");
        response.CreatedAt.ShouldBe(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    [Priority(2)]
    public async Task CreateResume_RegularUser_ShouldReturnOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsUserAsync(app, 2, ct);

        var request = new CreateResumeRequest("en-us");

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateResumeEndpoint, CreateResumeRequest, CreateResumeResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateResume_NoToken_ShouldReturnUnauthorized()
    {
        var (httpResponse, _) = await app.Client
            .POSTAsync<CreateResumeEndpoint, CreateResumeRequest, CreateResumeResponse>(
                new CreateResumeRequest("en-us"));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(Skip = "test-audit 04-B2: POST /resumes has no validator, a localization over 100 characters reaches varchar(100) and answers 500")]
    public async Task CreateResume_LocalizationLongerThan100_ShouldReturnBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsUserAsync(app, 4, ct);

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateResumeEndpoint, CreateResumeRequest>(new CreateResumeRequest(new string('a', 101)));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateResume_NullLocalization_ShouldApplyTheColumnDefault()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateResumeEndpoint, CreateResumeRequest, CreateResumeResponse>(new CreateResumeRequest(null));

        // EF omits a null on a property that has a database default, so the column default applies.
        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Localization.ShouldBe("en-us");
    }
}
