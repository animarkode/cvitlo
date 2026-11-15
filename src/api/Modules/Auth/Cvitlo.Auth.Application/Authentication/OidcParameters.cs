namespace Cvitlo.Auth.Application.Authentication;

internal static class OidcParameters
{
    public const string Prompt = "prompt";

    // kc_action is set only from the account-action allow-list.
    public const string KcAction = "kc_action";

    public const string AccountAction = ".cvitlo.account-action";

    public const string ReplacesSession = ".cvitlo.replaces-session";

    public const string AccountSubject = ".cvitlo.account-subject";

    // The ID token goes out as id_token_hint so Keycloak insists on the same user.
    public const string IdTokenHint = ".cvitlo.id-token-hint";

    public const string KcActionStatus = "kc_action_status";
}
