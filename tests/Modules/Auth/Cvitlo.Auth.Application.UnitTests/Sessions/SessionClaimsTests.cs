using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Cvitlo.Auth.Application.Sessions;
using Shouldly;

namespace Cvitlo.Auth.Application.UnitTests.Sessions;

public class SessionClaimsTests
{
    private const string RealmAccess = "realm_access";

    [Fact]
    public void ApplyAccessToken_TokenWithRoles_ShouldReplaceTheRealmAccessClaim()
    {
        var identity = new ClaimsIdentity([new Claim(RealmAccess, """{"roles":["old"]}""")], "test");
        var token = Unsigned(new Dictionary<string, object>(StringComparer.Ordinal) { [RealmAccess] = new { roles = new[] { "plan-pro" } } });

        SessionClaims.ApplyAccessToken(identity, token);

        var claim = identity.FindAll(RealmAccess).ShouldHaveSingleItem();
        using var json = JsonDocument.Parse(claim.Value);
        json.RootElement.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ShouldBe(["plan-pro"]);
    }

    [Fact]
    public void ApplyAccessToken_NullToken_ShouldOnlyRemoveTheStaleClaim()
    {
        var identity = new ClaimsIdentity([new Claim(RealmAccess, """{"roles":["old"]}"""), new Claim("sub", "u1")], "test");

        SessionClaims.ApplyAccessToken(identity, null);

        identity.FindAll(RealmAccess).ShouldBeEmpty();
        identity.FindFirst("sub")!.Value.ShouldBe("u1");
    }

    [Fact]
    public void ApplyIdToken_NewNames_ShouldReplaceProfileClaims()
    {
        var identity = new ClaimsIdentity([new Claim("given_name", "Old"), new Claim("family_name", "Name")], "test");
        var token = Unsigned(new Dictionary<string, object>(StringComparer.Ordinal) { ["given_name"] = "New", ["family_name"] = "Person" });

        SessionClaims.ApplyIdToken(identity, token);

        identity.FindAll("given_name").Select(c => c.Value).ShouldBe(["New"]);
        identity.FindAll("family_name").Select(c => c.Value).ShouldBe(["Person"]);
    }

    [Fact]
    public void ApplyIdToken_ClaimMissingFromTheToken_ShouldRemoveTheOldValue()
    {
        var identity = new ClaimsIdentity([new Claim("given_name", "Old"), new Claim("email", "a@b.test")], "test");
        var token = Unsigned(new Dictionary<string, object>(StringComparer.Ordinal) { ["email"] = "a@b.test" });

        SessionClaims.ApplyIdToken(identity, token);

        identity.FindAll("given_name").ShouldBeEmpty();
        identity.FindFirst("email")!.Value.ShouldBe("a@b.test");
    }

    [Fact]
    public void Roles_NoRealmAccess_ShouldReturnEmpty()
    {
        SessionClaims.Roles(new ClaimsPrincipal(new ClaimsIdentity("test"))).ShouldBeEmpty();
    }

    [Fact]
    public void Roles_RealmAccessWithoutRoles_ShouldReturnEmpty()
    {
        SessionClaims.Roles(Principal("{}")).ShouldBeEmpty();
    }

    [Fact]
    public void Roles_TwoRoles_ShouldReturnBoth()
    {
        SessionClaims.Roles(Principal("""{"roles":["a","b"]}""")).ShouldBe(["a", "b"]);
    }

    [Fact(Skip = "#156: SessionClaims.Roles throws InvalidOperationException when realm_access.roles is not an array")]
    public void Roles_RolesNotAnArray_ShouldReturnEmpty()
    {
        SessionClaims.Roles(Principal("""{"roles":"admin"}""")).ShouldBeEmpty();
    }

    private static ClaimsPrincipal Principal(string realmAccess)
    {
        return new ClaimsPrincipal(new ClaimsIdentity([new Claim(RealmAccess, realmAccess)], "test"));
    }

    private static string Unsigned(Dictionary<string, object> payload)
    {
        return $"{Base64Url("""{"alg":"none"}""")}.{Base64Url(JsonSerializer.Serialize(payload))}.";
    }

    private static string Base64Url(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
