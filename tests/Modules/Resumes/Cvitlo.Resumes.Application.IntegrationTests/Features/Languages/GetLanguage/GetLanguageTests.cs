using System.Net;
using Cvitlo.Resumes.Application.Features.Languages.CreateLanguage;
using Cvitlo.Resumes.Application.Features.Languages.GetAllLanguages;
using Cvitlo.Resumes.Application.Features.Languages.GetLanguage;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Languages.GetLanguage;

[Collection<CollectionResumes>]
public class GetLanguageTests(WebApp app) : TestBase
{
    [Fact]
    public async Task GetLanguage_NoToken_ShouldReturnUnauthorized()
    {
        var (httpResponse, _) = await app.Client
            .GETAsync<GetLanguageEndpoint, GetLanguageRequest, GetLanguageResponse>(
                new GetLanguageRequest(Guid.NewGuid(), Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetLanguage_ExistingItem_ShouldReturnTheCreatedLanguage()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = new CreateLanguageRequestFaker(resume.Id).Generate();

        var (_, languageResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateLanguageEndpoint, CreateLanguageRequest, CreateLanguageResponse>(createRequest);

        var (httpResponse, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetLanguageEndpoint, GetLanguageRequest, GetLanguageResponse>(
                new GetLanguageRequest(resume.Id, languageResponse.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        getResponse.Id.ShouldBe(languageResponse.Id);
        getResponse.Name.ShouldBe(createRequest.Name);
        getResponse.Level.ShouldBe(createRequest.Level);
    }

    [Fact]
    public async Task GetAllLanguages_ResumeWithTwoItems_ShouldReturnBoth()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var items = new CreateLanguageRequestFaker(resume.Id).Generate(2);

        await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateLanguageEndpoint, CreateLanguageRequest, CreateLanguageResponse>(items[0]);

        await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateLanguageEndpoint, CreateLanguageRequest, CreateLanguageResponse>(items[1]);

        var (httpResponse, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetAllLanguagesEndpoint, GetAllLanguagesRequest, GetAllLanguagesResponse>(
                new GetAllLanguagesRequest(resume.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        getResponse.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetLanguage_UnknownLanguage_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetLanguageEndpoint, GetLanguageRequest, GetLanguageResponse>(
                new GetLanguageRequest(resume.Id, Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllLanguages_UnknownResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetAllLanguagesEndpoint, GetAllLanguagesRequest, GetAllLanguagesResponse>(
                new GetAllLanguagesRequest(Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllLanguages_ResumeWithoutLanguages_ShouldReturnEmptyList()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var (httpResponse, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetAllLanguagesEndpoint, GetAllLanguagesRequest, GetAllLanguagesResponse>(
                new GetAllLanguagesRequest(resume.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        getResponse.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetLanguage_OtherUsersResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);

        var createRequest = new CreateLanguageRequestFaker(resume.Id).Generate();

        var (_, languageResponse) = await app.Client
            .WithAuthToken(userAToken)
            .POSTAsync<CreateLanguageEndpoint, CreateLanguageRequest, CreateLanguageResponse>(createRequest);

        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(userBToken)
            .GETAsync<GetLanguageEndpoint, GetLanguageRequest, GetLanguageResponse>(
                new GetLanguageRequest(resume.Id, languageResponse.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllLanguages_OtherUsersResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);

        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(userBToken)
            .GETAsync<GetAllLanguagesEndpoint, GetAllLanguagesRequest, GetAllLanguagesResponse>(
                new GetAllLanguagesRequest(resume.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetLanguage_OwnResumeWithAnotherUsersItem_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);
        var resumeA = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);
        var resumeB = await ResumesTestHelper.CreateResumeAsync(app.Client, userBToken, ct: ct);
        var item = await CreateLanguageAsync(userAToken, resumeA.Id);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(userBToken)
            .GETAsync<GetLanguageEndpoint, GetLanguageRequest, GetLanguageResponse>(
                new GetLanguageRequest(resumeB.Id, item.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetLanguage_LanguageOfTheCallersOtherResume_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resumeA = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var resumeA2 = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var item = await CreateLanguageAsync(accessToken, resumeA.Id);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetLanguageEndpoint, GetLanguageRequest, GetLanguageResponse>(
                new GetLanguageRequest(resumeA2.Id, item.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllLanguages_NoToken_ShouldReturnUnauthorized()
    {
        var (httpResponse, _) = await app.Client
            .GETAsync<GetAllLanguagesEndpoint, GetAllLanguagesRequest, GetAllLanguagesResponse>(
                new GetAllLanguagesRequest(Guid.NewGuid()));

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
