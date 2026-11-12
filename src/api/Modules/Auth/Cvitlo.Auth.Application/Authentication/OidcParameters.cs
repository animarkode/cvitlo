namespace Cvitlo.Auth.Application.Authentication;

internal static class OidcParameters
{
    public const string Prompt = "prompt";

    /// <summary>The Keycloak <c>kc_action</c> to run; set only from the account-action allow-list.</summary>
    public const string KcAction = "kc_action";

    /// <summary>The allow-listed account action name, kept so the callback knows what just ran.</summary>
    public const string AccountAction = ".cvitlo.account-action";

    /// <summary>Row id (hash of the key) of the web session this sign-in replaces.</summary>
    public const string ReplacesSession = ".cvitlo.replaces-session";

    /// <summary>The <c>sub</c> of the app session that started the action; the callback must belong to the same user.</summary>
    public const string AccountSubject = ".cvitlo.account-subject";

    /// <summary>The signed-in user's ID token, sent as <c>id_token_hint</c> so Keycloak insists on the same user.</summary>
    public const string IdTokenHint = ".cvitlo.id-token-hint";

    /// <summary>Query parameter on Keycloak's callback with the outcome of the action.</summary>
    public const string KcActionStatus = "kc_action_status";
}
