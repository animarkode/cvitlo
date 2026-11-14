using Cvitlo.Resumes.Application.Features.Persons.DeletePerson;
using System.Net;
using Cvitlo.Resumes.Application.Features.Persons.CreatePerson;
using Cvitlo.Resumes.Application.Features.Resumes.GetResume;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Persons.CreatePerson.Data;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Persons.CreatePerson;

[Collection<CollectionResumes>]
[Priority(10)]
public class CreatePersonTests(WebApp app) : TestBase
{
    [Theory]
    [ClassData(typeof(ValidPersonData))]
    [Priority(1)]
    public async Task CreatePerson_ValidRequest_ShouldReturnTheCreatedItem(CreatePersonRequest request)
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = request with { ResumeId = resume.Id };

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(createRequest);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Id.ShouldNotBe(Guid.Empty);
        response.ResumeId.ShouldBe(resume.Id);
        response.Firstname.ShouldBe(request.Firstname);
        response.Lastname.ShouldBe(request.Lastname);
        response.Email.ShouldBe(request.Email);
    }

    [Fact]
    [Priority(2)]
    public async Task CreatePerson_UnknownResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);

        var request = new CreatePersonRequestFaker(Guid.NewGuid()).Generate();

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [ClassData(typeof(InvalidPersonData))]
    [Priority(2)]
    public async Task CreatePerson_InvalidField_ShouldReturnBadRequest(
        CreatePersonRequest invalidRequest,
        string expectedKey)
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = invalidRequest with { ResumeId = resume.Id };

        var (httpResponse, error) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, ErrorResponse>(createRequest);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        error.Errors.Keys.ShouldBe([expectedKey]);
    }

    [Fact]
    [Priority(3)]
    public async Task CreatePerson_PersonAlreadyExists_ShouldReturnConflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var request = new CreatePersonRequestFaker(resume.Id).Generate();

        var (firstResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(request);

        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var (secondResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(request);

        secondResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    [Priority(4)]
    public async Task CreatePerson_WithName_ShouldSnapshotFullNameIntoResumeTitle()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var request = new CreatePersonRequestFaker(resume.Id).Generate() with
        {
            Firstname = "Ada",
            Lastname = "Lovelace"
        };

        var (createResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(request);

        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var (_, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(new GetResumeRequest(resume.Id));

        getResponse.Title.ShouldBe("Ada Lovelace");
    }

    [Fact]
    [Priority(5)]
    public async Task CreatePerson_WithMaximumLengthNames_ShouldTruncateResumeTitleToItsLimit()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var request = new CreatePersonRequestFaker(resume.Id).Generate() with
        {
            Firstname = new string('a', 100),
            Lastname = new string('b', 100)
        };

        var (createResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(request);

        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var (_, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(new GetResumeRequest(resume.Id));

        getResponse.Title.ShouldBe(new string('a', 100));
    }

    [Fact]
    [Priority(6)]
    public async Task CreatePerson_NoToken_ShouldReturnUnauthorized()
    {
        var (httpResponse, _) = await app.Client
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(
                new CreatePersonRequestFaker(Guid.NewGuid()).Generate());

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Priority(7)]
    public async Task CreatePerson_AfterDeletingThePerson_ShouldReturnOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var request = new CreatePersonRequestFaker(resume.Id).Generate();

        var (_, first) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(request);

        var deleteResponse = await app.Client
            .WithAuthToken(accessToken)
            .DELETEAsync<DeletePersonEndpoint, DeletePersonRequest>(new DeletePersonRequest(resume.Id, first.Id));

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (secondResponse, second) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(request);

        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.Id.ShouldNotBe(first.Id);
    }

    [Fact]
    [Priority(8)]
    public async Task CreatePerson_DateOfBirthToday_ShouldReturnOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(
                new CreatePersonRequestFaker(resume.Id).Generate() with { DateOfBirth = today });

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.DateOfBirth.ShouldBe(today);
    }

    [Fact]
    [Priority(9)]
    public async Task CreatePerson_AllFieldsNull_ShouldReturnOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var (httpResponse, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(
                new CreatePersonRequest(resume.Id, null, null, null, null, null, null, null, null, null, null, null, null));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Id.ShouldNotBe(Guid.Empty);
        response.Email.ShouldBeNull();
        response.Firstname.ShouldBeNull();
    }

    [Fact]
    [Priority(10)]
    public async Task CreatePerson_ValidRequest_ShouldNotLogContactDetails()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var marker = Guid.NewGuid().ToString("N");
        var request = new CreatePersonRequestFaker(resume.Id).Generate() with
        {
            Email = $"{marker}@pii.example",
            Phone = $"phone-{marker}",
            Street = $"street-{marker}"
        };

        var (httpResponse, _) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        app.Logs.Entries.Where(e => e.Contains(marker, StringComparison.Ordinal)).ShouldBeEmpty();
    }
}
