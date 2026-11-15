using ErrorOr;
using Cvitlo.Auth.Application.Sessions;

namespace Cvitlo.Auth.Application.Features.Account;

// Sign-in methods are stored passwords, stored passwordless passkeys and connected linked accounts;
// an authenticator app (otp) and a two-factor security key (webauthn) only complete a sign-in, so they are always removable.
public static class LastSignInMethodGuard
{
    public const string LastSignInMethodCode = "Account.LastSignInMethod";
    public const string CredentialNotFoundCode = "Account.CredentialNotFound";
    public const string LinkedAccountNotFoundCode = "Account.LinkedAccountNotFound";
    public const string NotRemovableCode = "Account.CredentialNotRemovable";

    private static readonly HashSet<string> SignInCredentialTypes = new(StringComparer.Ordinal)
    {
        "password",
        "webauthn-passwordless"
    };

    public static int CountSignInMethods(
        IReadOnlyList<KeycloakCredentialType> credentials,
        IReadOnlyList<KeycloakLinkedAccount> linkedAccounts)
    {
        var stored = credentials
            .Where(type => SignInCredentialTypes.Contains(type.Type))
            .Sum(type => StoredCredentials(type).Count());

        return stored + linkedAccounts.Count(account => account.Connected);
    }

    public static ErrorOr<Success> CheckCredentialRemoval(
        IReadOnlyList<KeycloakCredentialType> credentials,
        IReadOnlyList<KeycloakLinkedAccount> linkedAccounts,
        string credentialId)
    {
        var owner = credentials.FirstOrDefault(type =>
            StoredCredentials(type).Any(c => string.Equals(c.Id, credentialId, StringComparison.Ordinal)));

        if (owner is null)
        {
            return Error.NotFound(CredentialNotFoundCode, "The credential was not found.");
        }

        // Keycloak itself refuses these (a password, for one) with a bare 400; say why instead.
        if (!owner.Removeable)
        {
            return Error.Conflict(NotRemovableCode, "This kind of credential cannot be removed, only changed.");
        }

        return SignInCredentialTypes.Contains(owner.Type) && CountSignInMethods(credentials, linkedAccounts) <= 1
            ? LastSignInMethod()
            : Result.Success;
    }

    public static ErrorOr<Success> CheckUnlink(
        IReadOnlyList<KeycloakCredentialType> credentials,
        IReadOnlyList<KeycloakLinkedAccount> linkedAccounts,
        string alias)
    {
        var connected = linkedAccounts.Any(a =>
            a.Connected && string.Equals(a.ProviderAlias, alias, StringComparison.Ordinal));

        if (!connected)
        {
            return Error.NotFound(LinkedAccountNotFoundCode, "The linked account was not found.");
        }

        return CountSignInMethods(credentials, linkedAccounts) <= 1 ? LastSignInMethod() : Result.Success;
    }

    private static IEnumerable<KeycloakCredential> StoredCredentials(KeycloakCredentialType type)
    {
        return (type.UserCredentialMetadatas ?? [])
            .Select(container => container.Credential)
            .OfType<KeycloakCredential>();
    }

    private static Error LastSignInMethod()
    {
        return Error.Conflict(LastSignInMethodCode, "This is the only way to sign in to the account, so it cannot be removed.");
    }
}
