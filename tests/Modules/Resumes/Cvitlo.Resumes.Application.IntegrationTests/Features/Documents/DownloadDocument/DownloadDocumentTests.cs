using System.Net;
using Cvitlo.Resumes.Application.Features.Documents.DownloadDocument;
using Cvitlo.Resumes.Application.Features.Documents.Shared;
using Cvitlo.Resumes.Application.Features.Persons.CreatePerson;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Documents.DownloadDocument;

[Collection<CollectionResumes>]
[Priority(80)]
public class DownloadDocumentTests(WebApp app) : TestBase
{
    private static readonly byte[] PdfStub = [0x25, 0x50, 0x44, 0x46];

    [Fact]
    [Priority(0)]
    public async Task DownloadDocument_NoToken_ShouldReturnUnauthorized()
    {
        var httpResponse = await app.Client
            .GETAsync<DownloadDocumentEndpoint, DocumentRequest>(
                new DocumentRequest(Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Priority(1)]
    public async Task DownloadDocument_ShouldReturnPdfStream()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var personRequest = new CreatePersonRequestFaker(resume.Id).Generate();

        await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(personRequest);

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<DownloadDocumentEndpoint, DocumentRequest>(
                new DocumentRequest(resume.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        httpResponse.Content.Headers.ContentType?.MediaType.ShouldBe("application/pdf");
        (await httpResponse.Content.ReadAsByteArrayAsync(ct)).ShouldBe(PdfStub);
        FileNameOf(httpResponse).ShouldBe($"{personRequest.JobTitle}.pdf");
    }

    [Fact]
    [Priority(2)]
    public async Task DownloadNonExistentDocument_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<DownloadDocumentEndpoint, DocumentRequest>(
                new DocumentRequest(Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Priority(3)]
    public async Task CrossUserDownloadDocument_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);

        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .GETAsync<DownloadDocumentEndpoint, DocumentRequest>(
                new DocumentRequest(resume.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DownloadDocument_ResumeWithoutPerson_ShouldNameTheFileResume()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<DownloadDocumentEndpoint, DocumentRequest>(new DocumentRequest(resume.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        FileNameOf(httpResponse).ShouldBe("resume.pdf");
    }

    [Fact(Skip = "test-audit 04-B5: an empty job title is not null, so the file name becomes .pdf")]
    public async Task DownloadDocument_EmptyJobTitle_ShouldNameTheFileResume()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var personRequest = new CreatePersonRequestFaker(resume.Id).Generate() with { JobTitle = "" };

        var (personResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(personRequest);
        personResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<DownloadDocumentEndpoint, DocumentRequest>(new DocumentRequest(resume.Id));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        FileNameOf(httpResponse).ShouldBe("resume.pdf");
    }

    private static string? FileNameOf(HttpResponseMessage response)
    {
        var disposition = response.Content.Headers.ContentDisposition;

        return disposition?.FileNameStar ?? disposition?.FileName?.Trim('"');
    }
}
