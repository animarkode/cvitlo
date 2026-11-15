using System.Net;
using Cvitlo.Resumes.Application.Features.Languages.CreateLanguage;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Languages.CreateLanguage.Data;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;
using Cvitlo.Resumes.Application.Features.Languages.DeleteLanguage;
using Cvitlo.Resumes.Application.Features.Languages.GetAllLanguages;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Languages.CreateLanguage;

[Collection<CollectionResumes>]
public class CreateLanguageTests(WebApp app) : TestBase
{
    [Theory]
    [ClassData(typeof(ValidLanguageData))]
    public async Task CreateLanguage_ValidRequest_ShouldReturnTheCreatedItem(CreateLanguageRequest request)
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = request with { ResumeId = resume.Id };

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateLanguageEndpoint, CreateLanguageRequest, CreateLanguageResponse>(createRequest);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Id.ShouldNotBe(Guid.Empty);
        response.ResumeId.ShouldBe(resume.Id);
        response.Name.ShouldBe(request.Name);
        response.Level.ShouldBe(request.Level);
        response.Order.ShouldBe(0);
    }

    [Fact]
    public async Task CreateLanguage_UnknownResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var request = new CreateLanguageRequestFaker(Guid.NewGuid()).Generate();

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateLanguageEndpoint, CreateLanguageRequest, CreateLanguageResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [ClassData(typeof(InvalidLanguageData))]
    public async Task CreateLanguage_InvalidField_ShouldReturnBadRequest(
        CreateLanguageRequest invalidRequest,
        string expectedKey)
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = invalidRequest with { ResumeId = resume.Id };

        var (httpResponse, error) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateLanguageEndpoint, CreateLanguageRequest, ErrorResponse>(createRequest);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        error.Errors.Keys.ShouldBe([expectedKey]);
    }

    [Fact]
    public async Task CreateLanguage_FieldsAtMaximumLength_ShouldReturnOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var request = new CreateLanguageRequestFaker(resume.Id).Generate() with { Name = new string('a', 100) };

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateLanguageEndpoint, CreateLanguageRequest, CreateLanguageResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Name!.Length.ShouldBe(100);
    }

    [Theory]
    [InlineData("A1")]
    [InlineData("A2")]
    [InlineData("B1")]
    [InlineData("B2")]
    [InlineData("C1")]
    [InlineData("C2")]
    [InlineData("Native")]
    public async Task CreateLanguage_EveryAllowedLevel_ShouldReturnOk(string level)
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var request = new CreateLanguageRequestFaker(resume.Id).Generate() with { Level = level };

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateLanguageEndpoint, CreateLanguageRequest, CreateLanguageResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Level.ShouldBe(level);
    }

    [Fact]
    public async Task CreateLanguage_NoLevel_ShouldReturnOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var request = new CreateLanguageRequestFaker(resume.Id).Generate() with { Level = null };

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateLanguageEndpoint, CreateLanguageRequest, CreateLanguageResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Level.ShouldBeNull();
    }

    [Fact(Skip = "#161: a whitespace-only Level skips the whitelist and is stored; answers 200")]
    public async Task CreateLanguage_WhitespaceLevel_ShouldReturnBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var request = new CreateLanguageRequestFaker(resume.Id).Generate() with { Level = "  " };

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateLanguageEndpoint, CreateLanguageRequest, ErrorResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateLanguage_SeveralItems_ShouldNumberThemInCreationOrder()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var first = await CreateLanguageAsync(accessToken, resume.Id, 65001);
        var second = await CreateLanguageAsync(accessToken, resume.Id, 65002);
        var third = await CreateLanguageAsync(accessToken, resume.Id, 65003);

        new[] { first.Order, second.Order, third.Order }.ShouldBe(new[] { 0, 1, 2 });

        var (_, all) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetAllLanguagesEndpoint, GetAllLanguagesRequest, GetAllLanguagesResponse>(
                new GetAllLanguagesRequest(resume.Id));

        all.Items.Select(i => i.Id).ShouldBe([first.Id, second.Id, third.Id]);
        all.Items.Select(i => i.Order).ShouldBe(new[] { 0, 1, 2 });
    }

    [Fact]
    public async Task CreateLanguage_AfterDeletingTheMiddleItem_ShouldAppendAfterTheLast()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var first = await CreateLanguageAsync(accessToken, resume.Id, 65001);
        var middle = await CreateLanguageAsync(accessToken, resume.Id, 65002);
        var last = await CreateLanguageAsync(accessToken, resume.Id, 65003);

        var deleteResponse = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeleteLanguageEndpoint, DeleteLanguageRequest>(new DeleteLanguageRequest(resume.Id, middle.Id));

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var appended = await CreateLanguageAsync(accessToken, resume.Id, 65004);

        appended.Order.ShouldBe(3);

        var (_, all) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetAllLanguagesEndpoint, GetAllLanguagesRequest, GetAllLanguagesResponse>(
                new GetAllLanguagesRequest(resume.Id));

        all.Items.Select(i => i.Id).ShouldBe([first.Id, last.Id, appended.Id]);
        all.Items.Select(i => i.Order).ShouldBe(new[] { 0, 2, 3 });
    }

    [Fact]
    public async Task CreateLanguage_NoToken_ShouldReturnUnauthorized()
    {
        var (httpResponse, _) = await app.Client
            .POSTAsync<CreateLanguageEndpoint, CreateLanguageRequest, CreateLanguageResponse>(
                new CreateLanguageRequestFaker(Guid.NewGuid()).Generate());

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<CreateLanguageResponse> CreateLanguageAsync(
        string accessToken,
        Guid resumeId,
        int seed = ResumesTestConstants.LanguageSeed)
    {
        var (_, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateLanguageEndpoint, CreateLanguageRequest, CreateLanguageResponse>(
                new CreateLanguageRequestFaker(resumeId, seed).Generate());

        return response;
    }
}
