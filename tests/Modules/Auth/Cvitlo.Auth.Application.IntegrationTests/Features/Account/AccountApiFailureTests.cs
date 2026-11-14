using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Cvitlo.Common.Testing.Data;
using Shouldly;

namespace Cvitlo.Auth.Application.IntegrationTests.Features.Account;

[Collection<CollectionAuth>]
public class AccountApiFailureTests(WebApp app)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, "Account.Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, HttpStatusCode.Forbidden, "Account.Forbidden")]
    [InlineData(HttpStatusCode.NotFound, HttpStatusCode.NotFound, "Account.NotFound")]
    [InlineData(HttpStatusCode.InternalServerError, HttpStatusCode.BadGateway, "Account.Unavailable")]
    public async Task GetProfile_KeycloakAnswersWithAnError_ShouldMapIt(HttpStatusCode keycloak, HttpStatusCode expected, string code)
    {
        var (client, _) = await app.SignInAsync(app.Users[0], ct: Ct);
        using var _client = client;

        app.KeycloakAccountBackChannel.Replace = _ => new HttpResponseMessage(keycloak);
        try
        {
            using var response = await client.GetAsync("api/v1/auth/account/profile", Ct);

            response.StatusCode.ShouldBe(expected);
            (await ReadCodeAsync(response)).ShouldBe(code);
        }
        finally
        {
            app.KeycloakAccountBackChannel.Replace = null;
        }
    }

    [Fact]
    public async Task GetProfile_KeycloakTimesOut_ShouldReturnBadGateway()
    {
        var (client, _) = await app.SignInAsync(app.Users[0], ct: Ct);
        using var _client = client;

        app.KeycloakAccountBackChannel.Replace = _ => throw new TaskCanceledException("timeout", new TimeoutException());
        try
        {
            using var response = await client.GetAsync("api/v1/auth/account/profile", Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
            (await ReadCodeAsync(response)).ShouldBe("Account.Unavailable");
        }
        finally
        {
            app.KeycloakAccountBackChannel.Replace = null;
        }
    }

    [Theory]
    [InlineData("""{"errors":[{"field":"firstName","errorMessage":"first name refused"}]}""", "first name refused")]
    [InlineData("""{"errors":[{"field":"firstName","message":"first name refused"}]}""", "first name refused")]
    public async Task UpdateProfile_KeycloakRejectsAField_ShouldReturnValidationProblemForThatField(string body, string message)
    {
        var (client, _) = await app.SignInAsync(app.Users[0], ct: Ct);
        using var _client = client;

        // Only the write is refused, before Keycloak sees it; the profile is read as usual before and after.
        app.KeycloakAccountBackChannel.Replace = request => request.Method == HttpMethod.Post
            ? new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(body, Encoding.UTF8, "application/json") }
            : null;
        try
        {
            using var response = await client.PostAsJsonAsync("api/v1/auth/account/profile", new { firstName = "Eve" }, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            var problem = await response.Content.ReadFromJsonAsync<JsonObject>(Ct);
            problem!["errors"]!["firstName"]!.AsArray().Select(m => m!.GetValue<string>()).ShouldBe([message]);
        }
        finally
        {
            app.KeycloakAccountBackChannel.Replace = null;
        }
    }

    [Fact]
    public async Task UpdateProfile_KeycloakRefusesWithoutAField_ShouldReturnPlainBadRequest()
    {
        var (client, _) = await app.SignInAsync(app.Users[0], ct: Ct);
        using var _client = client;

        app.KeycloakAccountBackChannel.Replace = request => request.Method == HttpMethod.Post
            ? new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("""{"errorMessage":"Refused"}""", Encoding.UTF8, "application/json")
            }
            : null;
        try
        {
            using var response = await client.PostAsJsonAsync("api/v1/auth/account/profile", new { firstName = "Eve" }, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            var problem = await response.Content.ReadFromJsonAsync<JsonObject>(Ct);
            problem!["detail"]!.GetValue<string>().ShouldBe("Refused");
            problem["errors"].ShouldBeNull();
        }
        finally
        {
            app.KeycloakAccountBackChannel.Replace = null;
        }
    }

    [Fact]
    public async Task UpdateProfile_NameOf255Characters_ShouldReturnOk()
    {
        var (client, _) = await app.SignInAsync(await CreateUserAsync(5301), ct: Ct);
        using var _client = client;
        var name = new string('x', 255);

        using var response = await client.PostAsJsonAsync("api/v1/auth/account/profile", new { firstName = name }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<JsonObject>(Ct);
        updated!["firstName"]!.GetValue<string>().ShouldBe(name);
    }

    [Fact]
    public async Task UpdateProfile_NullNames_ShouldKeepTheCurrentNames()
    {
        var user = await CreateUserAsync(5302);
        var (client, _) = await app.SignInAsync(user, ct: Ct);
        using var _client = client;

        using var response = await client.PostAsJsonAsync("api/v1/auth/account/profile", new { }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<JsonObject>(Ct);
        updated!["firstName"]!.GetValue<string>().ShouldBe(user.FirstName);
        updated["lastName"]!.GetValue<string>().ShouldBe(user.LastName);
    }

    [Fact]
    public async Task UnlinkAccount_KeycloakAnswers500_ShouldReturnBadGatewayAndKeepTheLink()
    {
        var user = await CreateUserAsync(5303);
        var userId = (await app.Keycloak.Admin.FindUserIdAsync(user.Email, Ct))!;
        await app.Keycloak.Admin.LinkFederatedIdentityAsync(userId, WebApp.StandIn.Alias, $"standin-{userId}", user.Email, Ct);
        var (client, _) = await app.SignInAsync(user, ct: Ct);
        using var _client = client;

        // A password and a link: the guard allows the unlink, then Keycloak fails the delete itself.
        app.KeycloakAccountBackChannel.Replace = request => request.Method == HttpMethod.Delete
            ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
            : null;
        try
        {
            using var response = await client.DeleteAsync($"api/v1/auth/account/linked-accounts/{WebApp.StandIn.Alias}", Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
            (await ReadCodeAsync(response)).ShouldBe("Account.Unavailable");
        }
        finally
        {
            app.KeycloakAccountBackChannel.Replace = null;
        }

        (await app.Keycloak.Admin.GetFederatedIdentitiesAsync(userId, Ct)).Count.ShouldBe(1);
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>(Ct);

        return problem?["code"]?.GetValue<string>();
    }

    private async Task<TestUser> CreateUserAsync(int seed)
    {
        var user = new TestUserFaker(seed).Generate();
        await app.Keycloak.Admin.CreateUserAsync(user, ct: Ct);

        return user;
    }
}
