using ErrorOr;
using Cvitlo.Auth.Application.Features.Account;
using Cvitlo.Auth.Application.Sessions;
using Shouldly;

namespace Cvitlo.Auth.Application.UnitTests.Features.Account;

public class LastSignInMethodGuardTests
{
    [Fact]
    public void CountSignInMethods_PasswordPasskeyAndLink_ShouldCountEach()
    {
        List<KeycloakCredentialType> credentials =
        [
            Type("password", true, "pw1"),
            Type("webauthn-passwordless", true, "pk1", "pk2"),
            Type("otp", true, "otp1")
        ];
        List<KeycloakLinkedAccount> links = [Link("google", connected: true), Link("github", connected: false)];

        LastSignInMethodGuard.CountSignInMethods(credentials, links).ShouldBe(4);
    }

    [Fact]
    public void CheckCredentialRemoval_UnknownId_ShouldReturnNotFound()
    {
        var result = LastSignInMethodGuard.CheckCredentialRemoval([Type("password", true, "pw1")], [], "missing");

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Account.CredentialNotFound");
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public void CheckCredentialRemoval_TypeNotRemovable_ShouldReturnConflictNotRemovable()
    {
        List<KeycloakCredentialType> credentials = [Type("password", false, "pw1"), Type("webauthn-passwordless", true, "pk1")];

        var result = LastSignInMethodGuard.CheckCredentialRemoval(credentials, [], "pw1");

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Account.CredentialNotRemovable");
        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
    }

    [Fact]
    public void CheckCredentialRemoval_OnlyPasskey_ShouldReturnConflictLastSignInMethod()
    {
        var result = LastSignInMethodGuard.CheckCredentialRemoval([Type("webauthn-passwordless", true, "pk1")], [], "pk1");

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Account.LastSignInMethod");
        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
    }

    [Fact]
    public void CheckCredentialRemoval_PasskeyBesideAPassword_ShouldSucceed()
    {
        List<KeycloakCredentialType> credentials = [Type("password", false, "pw1"), Type("webauthn-passwordless", true, "pk1")];

        LastSignInMethodGuard.CheckCredentialRemoval(credentials, [], "pk1").IsError.ShouldBeFalse();
    }

    [Fact]
    public void CheckCredentialRemoval_OtpAsTheOnlyCredentialBesideOnePassword_ShouldSucceed()
    {
        List<KeycloakCredentialType> credentials = [Type("password", false, "pw1"), Type("otp", true, "otp1")];

        LastSignInMethodGuard.CheckCredentialRemoval(credentials, [], "otp1").IsError.ShouldBeFalse();
    }

    [Fact]
    public void CheckCredentialRemoval_LastPasskeyBesideAConnectedLink_ShouldSucceed()
    {
        var result = LastSignInMethodGuard.CheckCredentialRemoval(
            [Type("webauthn-passwordless", true, "pk1")], [Link("google", connected: true)], "pk1");

        result.IsError.ShouldBeFalse();
    }

    [Theory]
    [InlineData("github", true)]
    [InlineData("google", false)]
    public void CheckUnlink_NotConnected_ShouldReturnNotFound(string alias, bool absent)
    {
        List<KeycloakLinkedAccount> links = absent ? [Link("google", connected: true)] : [Link("google", connected: false)];

        var result = LastSignInMethodGuard.CheckUnlink([Type("password", false, "pw1")], links, alias);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Account.LinkedAccountNotFound");
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public void CheckUnlink_OnlySignInMethod_ShouldReturnConflictLastSignInMethod()
    {
        var result = LastSignInMethodGuard.CheckUnlink([], [Link("google", connected: true)], "google");

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Account.LastSignInMethod");
        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
    }

    [Fact]
    public void CheckUnlink_LinkBesideAPassword_ShouldSucceed()
    {
        LastSignInMethodGuard.CheckUnlink([Type("password", false, "pw1")], [Link("google", connected: true)], "google")
            .IsError.ShouldBeFalse();
    }

    [Fact]
    public void CheckUnlink_TwoLinksNoPassword_ShouldSucceed()
    {
        LastSignInMethodGuard.CheckUnlink([], [Link("google", connected: true), Link("github", connected: true)], "google")
            .IsError.ShouldBeFalse();
    }

    private static KeycloakCredentialType Type(string type, bool removeable, params string[] ids)
    {
        return new KeycloakCredentialType(
            type, null, null, null, null, null, removeable,
            ids.Select(id => new KeycloakCredentialContainer(new KeycloakCredential(id, type, null, null))).ToList());
    }

    private static KeycloakLinkedAccount Link(string alias, bool connected) => new(alias, null, connected, null);
}
