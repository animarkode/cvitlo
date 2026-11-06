using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Faber.Modules.Auth.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Faber.Modules.Auth.Application.Sessions;

/// <summary>The back-channel calls the BFF makes to Keycloak's token endpoints: refresh, logout and client credentials.</summary>
public sealed class KeycloakTokenClient(
    HttpClient http,
    IOptions<KeycloakOptions> options,
    ILogger<KeycloakTokenClient> logger)
{
    private readonly KeycloakOptions _options = options.Value;

    /// <summary>Requests fresh tokens for a session from Keycloak.</summary>
    /// <remarks>
    /// Only <c>invalid_grant</c> means the session is over (expired, signed out, revoked, or the refresh token
    /// already used). Everything else — an unreachable or slow Keycloak, <c>invalid_client</c> after a secret
    /// rotation, an unreadable answer — is a server-side problem, and the session is kept.
    /// </remarks>
    public async Task<RefreshResult> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        try
        {
            using var form = Form(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            });
            using var response = await http.PostAsync(_options.TokenEndpoint, form, ct);

            if (response.IsSuccessStatusCode)
            {
                var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);
                return tokens is null ? RefreshResult.Unavailable : RefreshResult.Refreshed(tokens);
            }

            var error = await ReadOAuthErrorAsync(response, ct);
            if (string.Equals(error, "invalid_grant", StringComparison.Ordinal))
            {
                return RefreshResult.Rejected;
            }

            logger.LogError("Keycloak refused a refresh with {StatusCode} {Error}; the session is kept",
                (int)response.StatusCode, error);
            return RefreshResult.Unavailable;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException
                                              || (exception is TaskCanceledException && !ct.IsCancellationRequested))
        {
            logger.LogError(exception, "Keycloak could not be asked to refresh; the session is kept");
            return RefreshResult.Unavailable;
        }
    }

    /// <summary>Requests an access token for a confidential client's own service account.</summary>
    /// <returns>The tokens, or <see langword="null"/> when Keycloak refused or could not be reached (logged).</returns>
    public async Task<TokenResponse?> GetClientCredentialsTokenAsync(
        string clientId, string clientSecret, CancellationToken ct)
    {
        try
        {
            using var form = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            });
            using var response = await http.PostAsync(_options.TokenEndpoint, form, ct);

            if (response.IsSuccessStatusCode)
            {
                var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);
                return string.IsNullOrEmpty(tokens?.AccessToken) ? null : tokens;
            }

            logger.LogError("Keycloak refused the client {ClientId}'s token request with {StatusCode} {Error}",
                clientId, (int)response.StatusCode, await ReadOAuthErrorAsync(response, ct));
            return null;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException
                                              || (exception is TaskCanceledException && !ct.IsCancellationRequested))
        {
            logger.LogError(exception, "Keycloak could not be asked for the client {ClientId}'s token", clientId);
            return null;
        }
    }

    private static async Task<string?> ReadOAuthErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            return body.ValueKind == JsonValueKind.Object && body.TryGetProperty("error", out var error)
                ? error.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Ends the Keycloak session the refresh token belongs to.</summary>
    public async Task<bool> LogoutAsync(string refreshToken, CancellationToken ct)
    {
        try
        {
            using var form = Form(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["refresh_token"] = refreshToken
            });
            using var response = await http.PostAsync(_options.LogoutEndpoint, form, ct);

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Keycloak logout failed");
            return false;
        }
    }

    private FormUrlEncodedContent Form(Dictionary<string, string> fields)
    {
        fields["client_id"] = _options.ClientId;
        fields["client_secret"] = _options.ClientSecret;
        return new FormUrlEncodedContent(fields);
    }
}
