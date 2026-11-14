using Cvitlo.Auth.Application.Features.AccountAction;
using Shouldly;

namespace Cvitlo.Auth.Application.UnitTests.Features.AccountAction;

public class AccountActionsTests
{
    [Theory]
    [InlineData("password")]
    [InlineData("passkey")]
    [InlineData("totp")]
    [InlineData("email")]
    [InlineData("link-google")]
    [InlineData("link-github")]
    [InlineData("reauthenticate")]
    public void IsAllowed_KnownName_ShouldBeTrue(string name)
    {
        AccountActions.IsAllowed(name).ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Password")]
    [InlineData("UPDATE_PASSWORD")]
    [InlineData("idp_link:google")]
    [InlineData("delete")]
    [InlineData("password ")]
    public void IsAllowed_AnythingElse_ShouldBeFalse(string? name)
    {
        AccountActions.IsAllowed(name).ShouldBeFalse();
    }

    [Theory]
    [InlineData("password", "UPDATE_PASSWORD")]
    [InlineData("passkey", "webauthn-register-passwordless")]
    [InlineData("totp", "CONFIGURE_TOTP")]
    [InlineData("email", "UPDATE_EMAIL")]
    [InlineData("link-google", "idp_link:google")]
    [InlineData("link-github", "idp_link:github")]
    public void TryGetKcAction_KnownName_ShouldReturnTheKeycloakAction(string name, string expected)
    {
        AccountActions.TryGetKcAction(name, out var kcAction).ShouldBeTrue();
        kcAction.ShouldBe(expected);
    }

    [Fact]
    public void TryGetKcAction_Reauthenticate_ShouldReturnFalse()
    {
        AccountActions.TryGetKcAction("reauthenticate", out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("SUCCESS")]
    [InlineData("ok")]
    public void NormalizeStatus_UnknownValue_ShouldReturnError(string? status)
    {
        AccountActions.NormalizeStatus(status).ShouldBe("error");
    }

    [Theory]
    [InlineData("success")]
    [InlineData("cancelled")]
    public void NormalizeStatus_KnownValue_ShouldPassThrough(string status)
    {
        AccountActions.NormalizeStatus(status).ShouldBe(status);
    }

    [Theory]
    [InlineData("password", "signing-in")]
    [InlineData("passkey", "signing-in")]
    [InlineData("totp", "signing-in")]
    [InlineData("email", "personal-info")]
    [InlineData("link-google", "linked-accounts")]
    [InlineData("link-github", "linked-accounts")]
    [InlineData("reauthenticate", "personal-info")]
    public void ReturnPath_EveryAction_ShouldPointAtItsSection(string action, string section)
    {
        AccountActions.ReturnPath(action, "success").ShouldBe($"/account/{section}?action={action}&status=success");
    }

    [Fact]
    public void ReturnPath_ActionWithReservedCharacters_ShouldEscapeThem()
    {
        AccountActions.ReturnPath("a&b=c", "error").ShouldContain("action=a%26b%3Dc");
    }
}
