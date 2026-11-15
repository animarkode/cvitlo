using System.Collections.Frozen;

namespace Cvitlo.Auth.Application.Features.AccountAction;

// Nothing from the request is forwarded to Keycloak: the route value is only a key into this list.
public static class AccountActions
{
    public const string StatusSuccess = "success";
    public const string StatusCancelled = "cancelled";
    public const string StatusError = "error";

    private static readonly FrozenDictionary<string, string> KcActions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["password"] = "UPDATE_PASSWORD",
        ["passkey"] = "webauthn-register-passwordless",
        ["totp"] = "CONFIGURE_TOTP",
        ["email"] = "UPDATE_EMAIL",
        ["link-google"] = "idp_link:google",
        ["link-github"] = "idp_link:github"
    }.ToFrozenDictionary(StringComparer.Ordinal);

    // prompt=login without a required action, so auth_time becomes fresh. No kc_action, hence no entry in the maps below.
    public const string Reauthenticate = "reauthenticate";

    public static bool IsAllowed(string? name)
    {
        return string.Equals(name, Reauthenticate, StringComparison.Ordinal) || (name is not null && KcActions.ContainsKey(name));
    }

    public static bool TryGetKcAction(string? name, out string kcAction)
    {
        kcAction = string.Empty;
        return name is not null && KcActions.TryGetValue(name, out kcAction!);
    }

    private static readonly FrozenDictionary<string, string> Names =
        KcActions.ToFrozenDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);

    public static string? FindName(string? kcAction)
    {
        return kcAction is not null && Names.TryGetValue(kcAction, out var name) ? name : null;
    }

    public static string NormalizeStatus(string? kcActionStatus)
    {
        return kcActionStatus switch
        {
            StatusSuccess => StatusSuccess,
            StatusCancelled => StatusCancelled,
            _ => StatusError
        };
    }

    private const string PersonalInfoSection = "personal-info";

    private static readonly FrozenDictionary<string, string> Sections = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["password"] = "signing-in",
        ["passkey"] = "signing-in",
        ["totp"] = "signing-in",
        ["email"] = PersonalInfoSection,
        ["link-google"] = "linked-accounts",
        ["link-github"] = "linked-accounts"
    }.ToFrozenDictionary(StringComparer.Ordinal);

    public static string ReturnPath(string action, string status)
    {
        var section = Sections.GetValueOrDefault(action, PersonalInfoSection);
        return $"/account/{section}?action={Uri.EscapeDataString(action)}&status={status}";
    }
}
