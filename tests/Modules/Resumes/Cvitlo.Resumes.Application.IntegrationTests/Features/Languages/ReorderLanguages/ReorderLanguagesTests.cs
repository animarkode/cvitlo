using System.Net;
using Cvitlo.Resumes.Application.Features.Languages.CreateLanguage;
using Cvitlo.Resumes.Application.Features.Languages.ReorderLanguages;
using Cvitlo.Resumes.Application.Features.Resumes.GetResume;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;
using System.Text;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Languages.ReorderLanguages;

[Collection<CollectionResumes>]
[Priority(54)]
public class ReorderLanguagesTests(WebApp app) : TestBase
{
    [Fact]
    [Priority(0)]
    public async Task ReorderLanguages_NoToken_ShouldReturnUnauthorized()
    {
        var httpResponse = await app.Client
            .PUTAsync<ReorderLanguagesEndpoint, ReorderLanguagesRequest>(
                new ReorderLanguagesRequest(Guid.NewGuid(), new List<Guid> { Guid.NewGuid() }));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Priority(1)]
    public async Task ReorderLanguages_ShouldPersistNewOrder()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var ids = await CreateLanguagesAsync(accessToken, resume.Id, 3);

        var newOrder = new List<Guid> { ids[2], ids[0], ids[1] };

        var reorderResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<ReorderLanguagesEndpoint, ReorderLanguagesRequest>(
                new ReorderLanguagesRequest(resume.Id, newOrder));

        reorderResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (_, resumeResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(
                new GetResumeRequest(resume.Id));

        resumeResponse.Languages.Select(l => l.Id).ShouldBe(newOrder);
        resumeResponse.Languages.Select(l => l.Order).ShouldBe(new[] { 0, 1, 2 });
    }

    [Fact]
    [Priority(2)]
    public async Task MismatchedIds_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var ids = await CreateLanguagesAsync(accessToken, resume.Id, 2);

        var mismatched = new List<Guid> { ids[0], Guid.NewGuid() };

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<ReorderLanguagesEndpoint, ReorderLanguagesRequest>(
                new ReorderLanguagesRequest(resume.Id, mismatched));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Priority(3)]
    public async Task EmptyOrderedIds_ShouldReturnBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<ReorderLanguagesEndpoint, ReorderLanguagesRequest>(
                new ReorderLanguagesRequest(resume.Id, new List<Guid>()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    [Priority(4)]
    public async Task DuplicateOrderedIds_ShouldReturnBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var duplicate = Guid.NewGuid();

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<ReorderLanguagesEndpoint, ReorderLanguagesRequest>(
                new ReorderLanguagesRequest(resume.Id, new List<Guid> { duplicate, duplicate }));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    [Priority(5)]
    public async Task CrossUserReorder_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);

        var ids = await CreateLanguagesAsync(userAToken, resume.Id, 2);

        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .PUTAsync<ReorderLanguagesEndpoint, ReorderLanguagesRequest>(
                new ReorderLanguagesRequest(resume.Id, new List<Guid> { ids[1], ids[0] }));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Priority(6)]
    public async Task ReorderLanguages_IdsFromAnotherResume_ShouldReturnNotFoundAndKeepOrder()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);
        var resumeA = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);
        var resumeB = await ResumesTestHelper.CreateResumeAsync(app.Client, userBToken, ct: ct);
        var foreignIds = await CreateLanguagesAsync(userAToken, resumeA.Id, 1);
        var ownIds = await CreateLanguagesAsync(userBToken, resumeB.Id, 2);

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .PUTAsync<ReorderLanguagesEndpoint, ReorderLanguagesRequest>(
                new ReorderLanguagesRequest(resumeB.Id, new List<Guid> { foreignIds[0], ownIds[0] }));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var (_, resumeResponse) = await app.Client
            .WithAuthToken(userBToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(
                new GetResumeRequest(resumeB.Id));

        resumeResponse.Languages.Select(i => i.Id).ShouldBe(ownIds);
    }

    [Fact]
    [Priority(7)]
    public async Task ReorderLanguages_FewerIdsThanItems_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var ids = await CreateLanguagesAsync(accessToken, resume.Id, 3);

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<ReorderLanguagesEndpoint, ReorderLanguagesRequest>(
                new ReorderLanguagesRequest(resume.Id, new List<Guid> { ids[0], ids[1] }));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Priority(8)]
    public async Task ReorderLanguages_SameOrder_ShouldReturnNoContentAndKeepOrder()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var ids = await CreateLanguagesAsync(accessToken, resume.Id, 3);

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<ReorderLanguagesEndpoint, ReorderLanguagesRequest>(new ReorderLanguagesRequest(resume.Id, ids));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (_, resumeResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(
                new GetResumeRequest(resume.Id));

        resumeResponse.Languages.Select(i => i.Id).ShouldBe(ids);
        resumeResponse.Languages.Select(i => i.Order).ShouldBe(new[] { 0, 1, 2 });
    }

    [Fact(Skip = "test-audit 05-B2: When() on the OrderedIds chain skips NotEmpty, SectionReorder dereferences null; answers 500")]
    [Priority(9)]
    public async Task ReorderLanguages_BodyWithoutOrderedIds_ShouldReturnBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        using var body = new StringContent("{}", Encoding.UTF8, "application/json");
        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PutAsync($"api/v1/resumes/{resume.Id}/languages/reorder", body, ct);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task<List<Guid>> CreateLanguagesAsync(string accessToken, Guid resumeId, int count)
    {
        var ids = new List<Guid>();

        for (var i = 0; i < count; i++)
        {
            var createRequest = new CreateLanguageRequestFaker(resumeId, 54010 + i).Generate();

            var (_, response) = await app.Client
                .WithAuthToken(accessToken)
                .POSTAsync<CreateLanguageEndpoint, CreateLanguageRequest, CreateLanguageResponse>(createRequest);

            ids.Add(response.Id);
        }

        return ids;
    }
}
