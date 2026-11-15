namespace Cvitlo.Auth.Application.Features.Login;

// Only an absolute path is allowed: a scheme (https:, javascript:) or a protocol-relative form (//host, /\host)
// falls back to the app root, so the sign-in link cannot become an open redirect.
public static class LocalReturnUrl
{
    public static string From(string? returnUrl)
    {
        var isLocalPath = !string.IsNullOrEmpty(returnUrl)
                          && returnUrl[0] == '/'
                          && (returnUrl.Length == 1 || (returnUrl[1] != '/' && returnUrl[1] != '\\'))
                          && !returnUrl.Any(char.IsControl);

        return isLocalPath ? returnUrl! : "/";
    }
}
