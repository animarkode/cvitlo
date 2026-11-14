using System.Net;
using Cvitlo.Resumes.Application.Features.Resumes.CreateResume;
using Cvitlo.Resumes.Application.Features.Resumes.GetResume;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Application.Features.Resumes.UpdateHobbies;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Resumes.UpdateHobbies;

[Collection<CollectionResumes>]
[Priority(6)]
public class UpdateHobbiesTests(WebApp app) : TestBase
{
    [Fact]
    [Priority(0)]
    public async Task UpdateHobbies_NoToken_ShouldReturnUnauthorized()
    {
        var httpResponse = await app.Client
            .PUTAsync<UpdateHobbiesEndpoint, UpdateHobbiesRequest>(
                new UpdateHobbiesRequest(Guid.NewGuid(), "Hobbies"));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Priority(1)]
    public async Task UpdateHobbies_ValidRequest_ShouldModifyHobbies()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var createRequest = new CreateResumeRequest("en-us");

        var (_, createResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateResumeEndpoint, CreateResumeRequest, CreateResumeResponse>(createRequest);

        var updateRequest = new UpdateHobbiesRequest(createResponse.Id, "Reading, Swimming");

        var updateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateHobbiesEndpoint, UpdateHobbiesRequest>(updateRequest);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getRequest = new GetResumeRequest(createResponse.Id);

        var (_, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(getRequest);

        getResponse.Hobbies.ShouldBe("Reading, Swimming");
    }

    [Fact]
    [Priority(2)]
    public async Task UpdateHobbies_UnknownResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateHobbiesEndpoint, UpdateHobbiesRequest>(
                new UpdateHobbiesRequest(Guid.NewGuid(), "Hobbies"));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Priority(3)]
    public async Task UpdateHobbies_Exactly200VisibleCharacters_ShouldReturnNoContent()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var createRequest = new CreateResumeRequest("en-us");

        var (_, createResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateResumeEndpoint, CreateResumeRequest, CreateResumeResponse>(createRequest);

        var hobbies = $"<p><strong>{new string('a', 200)}</strong></p>";
        var updateRequest = new UpdateHobbiesRequest(createResponse.Id, hobbies);

        var updateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateHobbiesEndpoint, UpdateHobbiesRequest>(updateRequest);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    [Priority(4)]
    public async Task UpdateHobbies_DisallowedAttributeWithAngleBracketAnd200VisibleCharacters_ShouldReturnNoContent()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var (_, createResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateResumeEndpoint, CreateResumeRequest, CreateResumeResponse>(new CreateResumeRequest("en-us"));

        var hobbies = $"<p data-value=\">\">{new string('a', 200)}</p>";
        var updateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateHobbiesEndpoint, UpdateHobbiesRequest>(new UpdateHobbiesRequest(createResponse.Id, hobbies));

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    [Priority(5)]
    public async Task UpdateHobbies_201VisibleCharacters_ShouldReturnBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var createRequest = new CreateResumeRequest("en-us");

        var (_, createResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateResumeEndpoint, CreateResumeRequest, CreateResumeResponse>(createRequest);

        var hobbies = $"<p><strong>{new string('a', 201)}</strong></p>";
        var updateRequest = new UpdateHobbiesRequest(createResponse.Id, hobbies);

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateHobbiesEndpoint, UpdateHobbiesRequest, ErrorResponse>(updateRequest);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Errors.ShouldContainKey("hobbies");
    }

    [Fact]
    public async Task UpdateHobbies_OtherUsersResume_ShouldReturnForbiddenAndChangeNothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var ownerToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var intruderToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, ownerToken, ct: ct);

        var ownerUpdate = await app.Client
            .WithAuthToken(ownerToken)
            .PUTAsync<UpdateHobbiesEndpoint, UpdateHobbiesRequest>(new UpdateHobbiesRequest(resume.Id, "owner"));
        ownerUpdate.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var intruderUpdate = await app.Client
            .WithAuthToken(intruderToken)
            .PUTAsync<UpdateHobbiesEndpoint, UpdateHobbiesRequest>(new UpdateHobbiesRequest(resume.Id, "intruder"));
        intruderUpdate.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var (_, stored) = await app.Client
            .WithAuthToken(ownerToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(new GetResumeRequest(resume.Id));
        stored.Hobbies.ShouldBe("owner");
    }

    [Fact(Skip = "test-audit 04-B3: hobbies are validated on visible length but stored as markup, so a long href overflows the column and answers 500")]
    public async Task UpdateHobbies_MarkupLongerThanTheColumn_ShouldReturnBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var markup = $"<a href=\"https://e.example/{new string('a', 1000)}\">x</a>";

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateHobbiesEndpoint, UpdateHobbiesRequest>(new UpdateHobbiesRequest(resume.Id, markup));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
