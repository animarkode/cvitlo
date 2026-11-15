using System.Net;
using System.Text;
using Cvitlo.Resumes.Application.Features.Resumes.GetResume;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Resumes;

[Collection<CollectionResumes>]
public class SingleFieldPutContractTests(WebApp app) : TestBase
{
    private const string KnownValue = "known";
    private const string KnownLocalization = "uk-ua";

    [Theory]
    [InlineData("title", Skip = "#145: a body without the named property binds it to null, the validator accepts it and the field is erased with 204")]
    [InlineData("summary", Skip = "#145: a body without the named property binds it to null, the validator accepts it and the field is erased with 204")]
    [InlineData("hobbies", Skip = "#145: a body without the named property binds it to null, the validator accepts it and the field is erased with 204")]
    [InlineData("localization")]
    public async Task Put_BodyWithValueProperty_ShouldNotEraseTheField(string field)
    {
        var (response, stored) = await PutThenReadAsync(field, """{"value":"x"}""");

        response.StatusCode.ShouldNotBe(HttpStatusCode.NoContent);
        stored.ShouldBe(KnownFor(field));
    }

    [Theory]
    [InlineData("title", Skip = "#145: a body without the named property binds it to null, the validator accepts it and the field is erased with 204")]
    [InlineData("summary", Skip = "#145: a body without the named property binds it to null, the validator accepts it and the field is erased with 204")]
    [InlineData("hobbies", Skip = "#145: a body without the named property binds it to null, the validator accepts it and the field is erased with 204")]
    [InlineData("localization")]
    public async Task Put_EmptyObjectBody_ShouldNotEraseTheField(string field)
    {
        var (response, stored) = await PutThenReadAsync(field, "{}");

        response.StatusCode.ShouldNotBe(HttpStatusCode.NoContent);
        stored.ShouldBe(KnownFor(field));
    }

    [Theory]
    [InlineData("title")]
    [InlineData("summary")]
    [InlineData("hobbies")]
    [InlineData("localization")]
    public async Task Put_NamedProperty_ShouldUpdateTheField(string field)
    {
        var newValue = string.Equals(field, "localization", StringComparison.Ordinal) ? "en-us" : "new";

        var (response, stored) = await PutThenReadAsync(field, $$"""{"{{field}}":"{{newValue}}"}""");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        stored.ShouldBe(newValue);
    }

    [Theory]
    [InlineData("title")]
    [InlineData("summary")]
    [InlineData("hobbies")]
    public async Task Put_ExplicitNull_ShouldClearTheField(string field)
    {
        var (response, stored) = await PutThenReadAsync(field, $$"""{"{{field}}":null}""");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        stored.ShouldBeNull();
    }

    private static string KnownFor(string field) => string.Equals(field, "localization", StringComparison.Ordinal) ? KnownLocalization : KnownValue;

    private async Task<(HttpResponseMessage Response, string? Stored)> PutThenReadAsync(string field, string json)
    {
        var ct = TestContext.Current.CancellationToken;
        var token = await ResumesTestHelper.SignInAsFirstUserAsync(app, ct);
        var resume = await ResumesTestHelper.CreateResumeAsync(app.Client, token, ct: ct);
        using var client = app.Client.WithAuthToken(token);

        var setup = await PutJsonAsync(client, resume.Id, field, $$"""{"{{field}}":"{{KnownFor(field)}}"}""");
        setup.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var response = await PutJsonAsync(client, resume.Id, field, json);

        var (_, resumeResponse) = await client
            .GETAsync<GetResumeEndpoint, GetResumeRequest, ResumeResponse>(new GetResumeRequest(resume.Id));

        return (response, FieldOf(resumeResponse, field));
    }

    private static string? FieldOf(ResumeResponse resume, string field) => field switch
    {
        "title" => resume.Title,
        "summary" => resume.Summary,
        "hobbies" => resume.Hobbies,
        "localization" => resume.Localization,
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, null)
    };

    private static async Task<HttpResponseMessage> PutJsonAsync(HttpClient client, Guid resumeId, string field, string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        return await client.PutAsync(
            $"api/v1/resumes/{resumeId}/{field}", content, TestContext.Current.CancellationToken);
    }
}
