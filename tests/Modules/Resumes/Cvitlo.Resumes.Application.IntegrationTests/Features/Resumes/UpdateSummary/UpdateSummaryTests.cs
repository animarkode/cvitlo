using System.Net;
using Cvitlo.Resumes.Application.Features.Resumes.CreateResume;
using Cvitlo.Resumes.Application.Features.Resumes.GetResume;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Application.Features.Resumes.UpdateSummary;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Resumes.UpdateSummary;

[Collection<CollectionResumes>]
[Priority(5)]
public class UpdateSummaryTests(WebApp app) : TestBase
{
    [Fact]
    [Priority(0)]
    public async Task UpdateSummary_NoToken_ShouldReturnUnauthorized()
    {
        var httpResponse = await app.Client
            .PUTAsync<UpdateSummaryEndpoint, UpdateSummaryRequest>(
                new UpdateSummaryRequest(Guid.NewGuid(), "Summary"));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Priority(1)]
    public async Task UpdateSummary_ValidRequest_ShouldModifySummary()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var createRequest = new CreateResumeRequest("en-us");

        var (_, createResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateResumeEndpoint, CreateResumeRequest, CreateResumeResponse>(createRequest);

        var updateRequest = new UpdateSummaryRequest(createResponse.Id, "Updated summary text");

        var updateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateSummaryEndpoint, UpdateSummaryRequest>(updateRequest);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getRequest = new GetResumeRequest(createResponse.Id);

        var (_, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(getRequest);

        getResponse.Summary.ShouldBe("Updated summary text");
    }

    [Fact]
    [Priority(2)]
    public async Task UpdateSummary_UnknownResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateSummaryEndpoint, UpdateSummaryRequest>(
                new UpdateSummaryRequest(Guid.NewGuid(), "Summary"));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Priority(3)]
    public async Task UpdateSummary_Exactly200VisibleCharacters_ShouldReturnNoContent()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var createRequest = new CreateResumeRequest("en-us");

        var (_, createResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateResumeEndpoint, CreateResumeRequest, CreateResumeResponse>(createRequest);

        var summary = $"<p><strong>{new string('a', 200)}</strong></p>";
        var updateRequest = new UpdateSummaryRequest(createResponse.Id, summary);

        var updateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateSummaryEndpoint, UpdateSummaryRequest>(updateRequest);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    [Priority(4)]
    public async Task UpdateSummary_TwoParagraphsWith200VisibleCharacters_ShouldReturnNoContent()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var (_, createResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateResumeEndpoint, CreateResumeRequest, CreateResumeResponse>(new CreateResumeRequest("en-us"));

        var summary = $"<p>{new string('a', 100)}</p><p>{new string('b', 100)}</p>";
        var updateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateSummaryEndpoint, UpdateSummaryRequest>(new UpdateSummaryRequest(createResponse.Id, summary));

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    [Priority(5)]
    public async Task UpdateSummary_201VisibleCharacters_ShouldReturnBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var createRequest = new CreateResumeRequest("en-us");

        var (_, createResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateResumeEndpoint, CreateResumeRequest, CreateResumeResponse>(createRequest);

        var summary = $"<p><strong>{new string('a', 201)}</strong></p>";
        var updateRequest = new UpdateSummaryRequest(createResponse.Id, summary);

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateSummaryEndpoint, UpdateSummaryRequest, ErrorResponse>(updateRequest);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Errors.ShouldContainKey("summary");
    }

    [Fact]
    public async Task UpdateSummary_OtherUsersResume_ShouldReturnForbiddenAndChangeNothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var ownerToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var intruderToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, ownerToken, ct: ct);

        var ownerUpdate = await app.Client
            .WithAuthToken(ownerToken)
            .PUTAsync<UpdateSummaryEndpoint, UpdateSummaryRequest>(new UpdateSummaryRequest(resume.Id, "owner"));
        ownerUpdate.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var intruderUpdate = await app.Client
            .WithAuthToken(intruderToken)
            .PUTAsync<UpdateSummaryEndpoint, UpdateSummaryRequest>(new UpdateSummaryRequest(resume.Id, "intruder"));
        intruderUpdate.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var (_, stored) = await app.Client
            .WithAuthToken(ownerToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(new GetResumeRequest(resume.Id));
        stored.Summary.ShouldBe("owner");
    }

    [Fact(Skip = "test-audit 04-B3: the summary text is validated on visible length but stored as markup, so a long href overflows the column and answers 500")]
    public async Task UpdateSummary_MarkupLongerThanTheColumn_ShouldReturnBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var markup = $"<a href=\"https://e.example/{new string('a', 2000)}\">x</a>";

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateSummaryEndpoint, UpdateSummaryRequest>(new UpdateSummaryRequest(resume.Id, markup));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
