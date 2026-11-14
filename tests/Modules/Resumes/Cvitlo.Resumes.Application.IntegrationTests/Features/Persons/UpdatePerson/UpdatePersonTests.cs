using System.Net;
using Cvitlo.Resumes.Application.Features.Persons.CreatePerson;
using Cvitlo.Resumes.Application.Features.Persons.GetPerson;
using Cvitlo.Resumes.Application.Features.Persons.UpdatePerson;
using Cvitlo.Resumes.Application.Features.Resumes.GetResume;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Application.Features.Resumes.UpdateTitle;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Persons.UpdatePerson.Data;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;

using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Persons.UpdatePerson;

[Collection<CollectionResumes>]
[Priority(12)]
public class UpdatePersonTests(WebApp app) : TestBase
{
    [Fact]
    [Priority(0)]
    public async Task UpdatePerson_NoToken_ShouldReturnUnauthorized()
    {
        var httpResponse = await app.Client
            .PUTAsync<UpdatePersonEndpoint, UpdatePersonRequest>(
                new UpdatePersonRequest(Guid.NewGuid(), Guid.NewGuid(), null, null, null, null, null, null, null, null, null, null, null, null));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Priority(1)]
    public async Task UpdatePerson_ValidRequest_ShouldModifyFields()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = new CreatePersonRequestFaker(resume.Id).Generate();

        var (_, personResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(createRequest);

        var updatedPerson = new CreatePersonRequestFaker(resume.Id, seed: 12001).Generate();
        var updateRequest = new UpdatePersonRequest(
            personResponse.Id,
            resume.Id,
            updatedPerson.JobTitle,
            updatedPerson.Firstname,
            updatedPerson.Lastname,
            updatedPerson.Email,
            updatedPerson.Phone,
            updatedPerson.Country,
            updatedPerson.City,
            updatedPerson.Street,
            updatedPerson.PostCode,
            updatedPerson.Nationality,
            updatedPerson.DateOfBirth,
            updatedPerson.DrivingLicense);

        var updateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdatePersonEndpoint, UpdatePersonRequest>(updateRequest);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (_, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetPersonEndpoint, GetPersonRequest, GetPersonResponse>(
                new GetPersonRequest(resume.Id, personResponse.Id));

        getResponse.Lastname.ShouldBe(updatedPerson.Lastname);
        getResponse.Email.ShouldBe(updatedPerson.Email);
        getResponse.JobTitle.ShouldBe(updatedPerson.JobTitle);
    }

    [Fact]
    [Priority(2)]
    public async Task UpdatePerson_UnknownPerson_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var person = new CreatePersonRequestFaker(resume.Id).Generate();
        var httpResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdatePersonEndpoint, UpdatePersonRequest>(
                new UpdatePersonRequest(
                    Guid.NewGuid(), resume.Id, person.JobTitle, person.Firstname, person.Lastname,
                    person.Email, person.Phone, person.Country, person.City, person.Street,
                    person.PostCode, person.Nationality, person.DateOfBirth, person.DrivingLicense));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Priority(3)]
    public async Task UpdatePerson_OtherUsersResume_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);

        var createRequest = new CreatePersonRequestFaker(resume.Id).Generate();

        var (_, personResponse) = await app.Client
            .WithAuthToken(userAToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(createRequest);

        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);

        var updated = new CreatePersonRequestFaker(resume.Id, seed: 12002).Generate();
        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .PUTAsync<UpdatePersonEndpoint, UpdatePersonRequest>(
                new UpdatePersonRequest(
                    personResponse.Id, resume.Id, updated.JobTitle, updated.Firstname, updated.Lastname,
                    updated.Email, updated.Phone, updated.Country, updated.City, updated.Street,
                    updated.PostCode, updated.Nationality, updated.DateOfBirth, updated.DrivingLicense));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [ClassData(typeof(InvalidUpdatePersonData))]
    [Priority(4)]
    public async Task UpdatePerson_InvalidField_ShouldReturnBadRequest(
        UpdatePersonRequest invalidRequest,
        string expectedKey)
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = new CreatePersonRequestFaker(resume.Id).Generate();
        var (_, createResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(createRequest);

        var updateRequest = invalidRequest with { Id = createResponse.Id, ResumeId = resume.Id };

        var (httpResponse, error) = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdatePersonEndpoint, UpdatePersonRequest, ErrorResponse>(updateRequest);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        error.Errors.Keys.ShouldBe([expectedKey]);
    }

    [Fact]
    [Priority(5)]
    public async Task UpdatePerson_FirstNameSetWhileTitleIsEmpty_ShouldSnapshotFullNameIntoResumeTitle()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = new CreatePersonRequestFaker(resume.Id).Generate() with
        {
            Firstname = null,
            Lastname = null
        };
        var (_, personResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(createRequest);

        var updateRequest = new UpdatePersonRequest(
            personResponse.Id,
            resume.Id,
            createRequest.JobTitle,
            "Ada",
            "Lovelace",
            createRequest.Email,
            createRequest.Phone,
            createRequest.Country,
            createRequest.City,
            createRequest.Street,
            createRequest.PostCode,
            createRequest.Nationality,
            createRequest.DateOfBirth,
            createRequest.DrivingLicense);

        var updateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdatePersonEndpoint, UpdatePersonRequest>(updateRequest);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (_, getResumeResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(new GetResumeRequest(resume.Id));

        getResumeResponse.Title.ShouldBe("Ada Lovelace");
    }

    [Fact]
    [Priority(6)]
    public async Task UpdatePerson_NameChangedAfterTitleSnapshot_ShouldLeaveResumeTitleUnchanged()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = new CreatePersonRequestFaker(resume.Id).Generate() with
        {
            Firstname = null,
            Lastname = null
        };
        var (_, personResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(createRequest);

        var firstUpdateRequest = new UpdatePersonRequest(
            personResponse.Id,
            resume.Id,
            createRequest.JobTitle,
            "Ada",
            "Lovelace",
            createRequest.Email,
            createRequest.Phone,
            createRequest.Country,
            createRequest.City,
            createRequest.Street,
            createRequest.PostCode,
            createRequest.Nationality,
            createRequest.DateOfBirth,
            createRequest.DrivingLicense);

        await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdatePersonEndpoint, UpdatePersonRequest>(firstUpdateRequest);

        var secondUpdateRequest = firstUpdateRequest with { Firstname = "Grace", Lastname = "Hopper" };

        var secondUpdateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdatePersonEndpoint, UpdatePersonRequest>(secondUpdateRequest);

        secondUpdateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (_, getResumeResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(new GetResumeRequest(resume.Id));

        getResumeResponse.Title.ShouldBe("Ada Lovelace");
    }

    [Fact]
    [Priority(7)]
    public async Task UpdatePerson_ManualTitleSet_ShouldLeaveManualTitleUnchanged()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = new CreatePersonRequestFaker(resume.Id).Generate();
        var (_, personResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(createRequest);

        await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdateTitleEndpoint, UpdateTitleRequest>(new UpdateTitleRequest(resume.Id, "Manual Title"));

        var updateRequest = new UpdatePersonRequest(
            personResponse.Id,
            resume.Id,
            createRequest.JobTitle,
            "Ada",
            "Lovelace",
            createRequest.Email,
            createRequest.Phone,
            createRequest.Country,
            createRequest.City,
            createRequest.Street,
            createRequest.PostCode,
            createRequest.Nationality,
            createRequest.DateOfBirth,
            createRequest.DrivingLicense);

        var updateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdatePersonEndpoint, UpdatePersonRequest>(updateRequest);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (_, getResumeResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(new GetResumeRequest(resume.Id));

        getResumeResponse.Title.ShouldBe("Manual Title");
    }

    [Fact]
    [Priority(8)]
    public async Task UpdatePerson_BlankNames_ShouldLeaveResumeTitleNull()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);

        var createRequest = new CreatePersonRequestFaker(resume.Id).Generate() with
        {
            Firstname = null,
            Lastname = null
        };
        var (_, personResponse) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(createRequest);

        var updateRequest = new UpdatePersonRequest(
            personResponse.Id,
            resume.Id,
            createRequest.JobTitle,
            null,
            null,
            createRequest.Email,
            createRequest.Phone,
            createRequest.Country,
            createRequest.City,
            createRequest.Street,
            createRequest.PostCode,
            createRequest.Nationality,
            createRequest.DateOfBirth,
            createRequest.DrivingLicense);

        var updateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdatePersonEndpoint, UpdatePersonRequest>(updateRequest);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (_, getResumeResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(new GetResumeRequest(resume.Id));

        getResumeResponse.Title.ShouldBeNull();
    }

    [Fact]
    [Priority(9)]
    public async Task UpdatePerson_OwnResumeWithAnotherUsersPerson_ShouldReturnNotFoundAndChangeNothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var userAToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var userBToken = await ResumesTestHelper.SignInAsUserAsync(app, 1, ct);
        var resumeA = await ResumesTestHelper.CreateResumeAsync(app.Client, userAToken, ct: ct);
        var resumeB = await ResumesTestHelper.CreateResumeAsync(app.Client, userBToken, ct: ct);
        var person = await CreatePersonAsync(userAToken, resumeA.Id);
        var changed = new CreatePersonRequestFaker(resumeB.Id, 12901).Generate();

        var httpResponse = await app.Client
            .WithAuthToken(userBToken)
            .PUTAsync<UpdatePersonEndpoint, UpdatePersonRequest>(
                new UpdatePersonRequest(
                    person.Id, resumeB.Id, changed.JobTitle, changed.Firstname, changed.Lastname,
                    changed.Email, changed.Phone, changed.Country, changed.City, changed.Street,
                    changed.PostCode, changed.Nationality, changed.DateOfBirth, changed.DrivingLicense));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var (_, getResponse) = await app.Client
            .WithAuthToken(userAToken)
            .GETAsync<GetPersonEndpoint, GetPersonRequest, GetPersonResponse>(
                new GetPersonRequest(resumeA.Id, person.Id));

        getResponse.Email.ShouldBe(person.Email);
    }

    [Fact]
    [Priority(10)]
    public async Task UpdatePerson_FieldsAtMaximumLength_ShouldReturnNoContent()
    {
        var ct = TestContext.Current.CancellationToken;
        var accessToken = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, accessToken, ct: ct);
        var person = await CreatePersonAsync(accessToken, resume.Id);
        var text = new string('a', 100);
        var email = new string('a', 91) + "@test.com";

        var updateResponse = await app.Client
            .WithAuthToken(accessToken)
            .PUTAsync<UpdatePersonEndpoint, UpdatePersonRequest>(
                new UpdatePersonRequest(
                    person.Id, resume.Id, text, text, text, email, text, text, text, text, text, text,
                    new DateOnly(1990, 1, 1), text));

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (_, getResponse) = await app.Client
            .WithAuthToken(accessToken)
            .GETAsync<GetPersonEndpoint, GetPersonRequest, GetPersonResponse>(
                new GetPersonRequest(resume.Id, person.Id));

        getResponse.Email.ShouldBe(email);
        getResponse.JobTitle!.Length.ShouldBe(100);
        getResponse.DrivingLicense!.Length.ShouldBe(100);
        getResponse.Street!.Length.ShouldBe(100);
    }

    private async Task<CreatePersonResponse> CreatePersonAsync(string accessToken, Guid resumeId, int seed = ResumesTestConstants.PersonSeed)
    {
        var (_, response) = await app.Client
            .WithAuthToken(accessToken)
            .POSTAsync<CreatePersonEndpoint, CreatePersonRequest, CreatePersonResponse>(
                new CreatePersonRequestFaker(resumeId, seed).Generate());

        return response;
    }
}
