using Cvitlo.Resumes.Application.Features.Resumes.CreateResume;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;

public static class ResumesTestHelper
{
    public static Task<string> SignInAsFirstUserAsync(WebApp app, CancellationToken ct = default)
    {
        return SignInAsUserAsync(app, 0, ct);
    }

    // Gets a Keycloak access token for one of the fixture's users. Resume endpoints are exercised as a bearer
    // client (the path a mobile client takes); the browser session path is covered by the Auth tests.
    public static Task<string> SignInAsUserAsync(WebApp app, int userIndex, CancellationToken ct = default)
    {
        return app.GetAccessTokenAsync(app.Users[userIndex], ct);
    }

    public static async Task<CreateResumeResponse> CreateResumeAsync(
        HttpClient client,
        string accessToken,
        string localization = "en-us",
        CancellationToken ct = default)
    {
        var (_, response) = await client
            .WithAuthToken(accessToken)
            .POSTAsync<CreateResumeEndpoint, CreateResumeRequest, CreateResumeResponse>(
                new CreateResumeRequest(localization));

        return response;
    }
}
