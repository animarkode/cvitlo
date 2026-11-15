using Cvitlo.Common.Testing.Data;

namespace Cvitlo.Api.IntegrationTests;

public static class WebAppExtensions
{
    // Rate-limit budgets are per user and live as long as the app, so a test that spends one needs
    // an account no other test has touched. Seeds must be unique across the project.
    public static async Task<string> SignInNewUserAsync(this WebApp app, int seed, CancellationToken ct)
    {
        var user = new TestUserFaker(seed).Generate();
        await app.Keycloak.Admin.CreateUserAsync(user, ct: ct);

        return await app.GetAccessTokenAsync(user, ct);
    }
}
