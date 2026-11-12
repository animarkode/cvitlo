using Cvitlo.Auth.Application.Sessions;
using Cvitlo.Common.Testing.Data;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Cvitlo.Auth.Application.IntegrationTests.Features.Account;

[Collection<CollectionAuth>]
public class KeycloakAdminClientTests(WebApp app)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task DeleteUser_ShouldRemoveTheUserAndSucceedAgain()
    {
        var user = new TestUserFaker(9137).Generate();
        var id = await app.Keycloak.Admin.CreateUserAsync(user, ct: Ct);
        using var scope = app.Services.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<KeycloakAdminClient>();

        var first = await client.DeleteUserAsync(id, Ct);
        var second = await client.DeleteUserAsync(id, Ct);

        first.IsError.ShouldBeFalse();
        second.IsError.ShouldBeFalse("a user Keycloak no longer knows counts as deleted");
        (await app.Keycloak.Admin.FindUserIdAsync(user.Email, Ct)).ShouldBeNull();
    }
}
