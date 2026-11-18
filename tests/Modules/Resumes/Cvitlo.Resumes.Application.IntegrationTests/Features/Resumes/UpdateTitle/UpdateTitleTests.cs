using System.Net;
using Cvitlo.Resumes.Application.Features.Resumes.CreateResume;
using Cvitlo.Resumes.Application.Features.Resumes.GetResume;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Application.Features.Resumes.UpdateTitle;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Resumes.UpdateTitle;

[Collection<CollectionResumes>]
public class UpdateTitleTests(WebApp app) : TestBase
{
    [Fact]
    public async Task UpdateTitle_NoToken_ShouldReturnUnauthorized()
    {
        var httpResponse = await app.Client
            .PUTAsync<UpdateTitleEndpoint, UpdateTitleRequest>(
                new UpdateTitleRequest(Guid.NewGuid(), "Title"));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateTitle_ValidRequest_ShouldModifyTitle()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var updateRequest = new UpdateTitleRequest(resume.Id, "My Custom Title");

        var updateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateTitleEndpoint, UpdateTitleRequest>(updateRequest);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (_, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(new GetResumeRequest(resume.Id));

        getResponse.Title.ShouldBe("My Custom Title");
    }

    [Fact]
    public async Task UpdateTitle_UnknownResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateTitleEndpoint, UpdateTitleRequest>(
                new UpdateTitleRequest(Guid.NewGuid(), "Title"));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateTitle_101Characters_ShouldReturnBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var updateRequest = new UpdateTitleRequest(resume.Id, new string('a', 101));

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateTitleEndpoint, UpdateTitleRequest, ErrorResponse>(updateRequest);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Errors.ShouldContainKey("title");
    }

    [Fact]
    public async Task UpdateTitle_WhitespaceTitle_ShouldClearTheTitle()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var updateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateTitleEndpoint, UpdateTitleRequest>(new UpdateTitleRequest(resume.Id, "   "));

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (_, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(new GetResumeRequest(resume.Id));

        getResponse.Title.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateTitle_OtherUsersResume_ShouldReturnForbiddenAndChangeNothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var ownerToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var intruderToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, ownerToken, ct: ct);

        var ownerUpdate = await app.Client
            .WithAuthToken(ownerToken)
            .PUTAsync<UpdateTitleEndpoint, UpdateTitleRequest>(new UpdateTitleRequest(resume.Id, "owner"));
        ownerUpdate.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var intruderUpdate = await app.Client
            .WithAuthToken(intruderToken)
            .PUTAsync<UpdateTitleEndpoint, UpdateTitleRequest>(new UpdateTitleRequest(resume.Id, "intruder"));
        intruderUpdate.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var (_, stored) = await app.Client
            .WithAuthToken(ownerToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(new GetResumeRequest(resume.Id));
        stored.Title.ShouldBe("owner");
    }

    [Fact]
    public async Task UpdateTitle_Exactly100Characters_ShouldReturnNoContent()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateTitleEndpoint, UpdateTitleRequest>(
                new UpdateTitleRequest(resume.Id, new string('a', 100)));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateTitle_PaddedTitle_ShouldStoreItTrimmed()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var updateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateTitleEndpoint, UpdateTitleRequest>(new UpdateTitleRequest(resume.Id, "  My CV  "));
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (_, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(new GetResumeRequest(resume.Id));

        getResponse.Title.ShouldBe("My CV");
    }
}
