using System.Net;
using Cvitlo.Documents.PublicApi;
using Cvitlo.Documents.PublicApi.Resumes;
using Cvitlo.Resumes.Application.Features.Documents.DownloadDocument;
using Cvitlo.Resumes.Application.Features.Documents.GenerateDocument;
using Cvitlo.Resumes.Application.Features.Documents.Shared;
using Cvitlo.Resumes.Application.Features.Persons.CreatePerson;
using Cvitlo.Resumes.Application.Features.Persons.UpdatePerson;
using Cvitlo.Resumes.Application.Features.Skills.CreateSkill;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Resumes.Domain.Entities;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Documents.Caching;

[Collection<CollectionResumes>]
[Priority(91)]
public class ResumePdfCacheTests(WebApp app) : TestBase
{
    [Fact]
    [Priority(1)]
    public async Task DownloadDocument_Twice_ShouldRenderOnce()
    {
        app.Services.GetRequiredService<IDocumentsModuleApi>().ClearReceivedCalls();
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(
                new CreatePersonRequestFaker(resume.Id).Generate());

        await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<DownloadDocumentEndpoint, DocumentRequest>(new DocumentRequest(resume.Id));

        var secondResponse = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<DownloadDocumentEndpoint, DocumentRequest>(new DocumentRequest(resume.Id));

        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        AssertRenderCount(1);
    }

    [Fact]
    [Priority(2)]
    public async Task GenerateDocument_AfterDownload_ShouldReuseRenderedPdf()
    {
        app.Services.GetRequiredService<IDocumentsModuleApi>().ClearReceivedCalls();
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(
                new CreatePersonRequestFaker(resume.Id).Generate());

        var downloadResponse = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<DownloadDocumentEndpoint, DocumentRequest>(new DocumentRequest(resume.Id));

        var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync(ct);

        var (_, generateResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<GenerateDocumentEndpoint, DocumentRequest, GenerateDocumentResponse>(
                new DocumentRequest(resume.Id));

        Convert.FromBase64String(generateResponse.Base64Pdf).ShouldBe(downloadedBytes);
        AssertRenderCount(1);
    }

    [Fact]
    [Priority(3)]
    public async Task DownloadDocument_AfterPersonUpdate_ShouldRenderAgain()
    {
        app.Services.GetRequiredService<IDocumentsModuleApi>().ClearReceivedCalls();
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var (_, person) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(
                new CreatePersonRequestFaker(resume.Id).Generate());

        await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<DownloadDocumentEndpoint, DocumentRequest>(new DocumentRequest(resume.Id));

        var updateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdatePersonEndpoint, UpdatePersonRequest>(
                new UpdatePersonRequest(
                    person.Id,
                    resume.Id,
                    "Updated Job Title",
                    person.Firstname,
                    person.Lastname,
                    person.Email,
                    person.Phone,
                    person.Country,
                    person.City,
                    person.Street,
                    person.PostCode,
                    person.Nationality,
                    person.DateOfBirth,
                    person.DrivingLicense));

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var secondResponse = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<DownloadDocumentEndpoint, DocumentRequest>(new DocumentRequest(resume.Id));

        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        AssertRenderCount(2);
    }

    [Fact]
    [Priority(4)]
    public async Task DownloadDocument_AfterSkillCreate_ShouldRenderAgain()
    {
        app.Services.GetRequiredService<IDocumentsModuleApi>().ClearReceivedCalls();
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(
                new CreatePersonRequestFaker(resume.Id).Generate());

        await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<DownloadDocumentEndpoint, DocumentRequest>(new DocumentRequest(resume.Id));

        await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateSkillEndpoint, CreateSkillRequest, CreateSkillResponse>(
                new CreateSkillRequestFaker(resume.Id).Generate());

        var secondResponse = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<DownloadDocumentEndpoint, DocumentRequest>(new DocumentRequest(resume.Id));

        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        AssertRenderCount(2);
    }

    private void AssertRenderCount(int expectedCount)
    {
        var documentsModuleApi = app.Services.GetRequiredService<IDocumentsModuleApi>();

        _ = documentsModuleApi.Received(expectedCount).RenderResumeToPdfAsync(
            Arg.Any<ResumeDocument>(),
            ResumeTemplate.First,
            Arg.Any<CancellationToken>());
    }
}
