using System.Net;
using Cvitlo.Resumes.Application.Features.Documents.GenerateDocument;
using Cvitlo.Resumes.Application.Features.Documents.Shared;
using Cvitlo.Resumes.Application.Features.Persons.CreatePerson;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Documents.GenerateDocument;

[Collection<CollectionResumes>]
public class GenerateDocumentTests(WebApp app) : TestBase
{
    [Fact]
    public async Task GenerateDocument_NoToken_ShouldReturnUnauthorized()
    {
        var (httpResponse, _) = await app.Client
            .POSTAsync<GenerateDocumentEndpoint, DocumentRequest, GenerateDocumentResponse>(
                new DocumentRequest(Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GenerateDocument_ResumeWithPerson_ShouldReturnBase64Pdf()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var personRequest = new CreatePersonRequestFaker(resume.Id).Generate();

        await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(personRequest);

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<GenerateDocumentEndpoint, DocumentRequest, GenerateDocumentResponse>(
                new DocumentRequest(resume.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        Convert.FromBase64String(response.Base64Pdf).ShouldBe([0x25, 0x50, 0x44, 0x46]);
    }

    [Fact]
    public async Task GenerateDocument_UnknownResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<GenerateDocumentEndpoint, DocumentRequest, GenerateDocumentResponse>(
                new DocumentRequest(Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GenerateDocument_OtherUsersResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);

        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);

        var (httpResponse, _) = await app.Client
            .WithAuthToken(userBToken)
            .POSTAsync<GenerateDocumentEndpoint, DocumentRequest, GenerateDocumentResponse>(
                new DocumentRequest(resume.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
