using System.Net;
using System.Text.Json;
using ErrorOr;
using Cvitlo.Auth.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cvitlo.Auth.Application.Sessions;

/// <summary>
/// Keycloak's Admin API, called as the <c>faber-admin</c> client's service account, which holds only the
/// <c>manage-users</c> role. It exists to delete a user's own account; failures come back as errors, never
/// as exceptions.
/// </summary>
public sealed class KeycloakAdminClient(
    HttpClient http,
    KeycloakTokenClient tokenClient,
    IOptions<KeycloakOptions> options,
    ILogger<KeycloakAdminClient> logger)
{
    /// <summary>The error code for every failure to reach or use the Admin API.</summary>
    public const string UnavailableErrorCode = "Account.Unavailable";

    /// <summary>
    /// Deletes a user and, with them, everything Keycloak holds for them. A user Keycloak no longer knows
    /// counts as deleted, so a retry after a half-finished deletion succeeds.
    /// </summary>
    /// <param name="subject">The Keycloak user id (the token's <c>sub</c>).</param>
    /// <param name="ct">Cancels the calls.</param>
    public async Task<ErrorOr<Deleted>> DeleteUserAsync(string subject, CancellationToken ct)
    {
        try
        {
            var token = await GetAccessTokenAsync(ct);
            if (token.IsError)
            {
                return token.Errors;
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Delete, $"{options.Value.AdminUsersUrl}/{Uri.EscapeDataString(subject)}");
            request.Headers.Authorization = new("Bearer", token.Value);

            using var response = await http.SendAsync(request, ct);
            if (response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.NotFound)
            {
                return Result.Deleted;
            }

            logger.LogError("Keycloak's Admin API answered {StatusCode} to a user deletion", (int)response.StatusCode);
            return Unavailable();
        }
        catch (Exception exception) when (IsTransient(exception, ct))
        {
            return Unavailable(exception);
        }
    }

    /// <summary>Asks whether Keycloak still knows a user. Viewing is part of <c>manage-users</c>.</summary>
    /// <param name="subject">The Keycloak user id (the token's <c>sub</c>).</param>
    /// <param name="ct">Cancels the calls.</param>
    public async Task<ErrorOr<bool>> UserExistsAsync(string subject, CancellationToken ct)
    {
        try
        {
            var token = await GetAccessTokenAsync(ct);
            if (token.IsError)
            {
                return token.Errors;
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Get, $"{options.Value.AdminUsersUrl}/{Uri.EscapeDataString(subject)}");
            request.Headers.Authorization = new("Bearer", token.Value);

            using var response = await http.SendAsync(request, ct);
            if (response.StatusCode is HttpStatusCode.OK)
            {
                return true;
            }

            if (response.StatusCode is HttpStatusCode.NotFound)
            {
                return false;
            }

            logger.LogError("Keycloak's Admin API answered {StatusCode} to a user lookup", (int)response.StatusCode);
            return Unavailable();
        }
        catch (Exception exception) when (IsTransient(exception, ct))
        {
            return Unavailable(exception);
        }
    }

    private async Task<ErrorOr<string>> GetAccessTokenAsync(CancellationToken ct)
    {
        var keycloak = options.Value;
        var tokens = await tokenClient.GetClientCredentialsTokenAsync(keycloak.AdminClientId, keycloak.AdminClientSecret, ct);

        return tokens is null ? Unavailable() : tokens.AccessToken;
    }

    private static bool IsTransient(Exception exception, CancellationToken ct)
    {
        return exception is HttpRequestException or JsonException
               || (exception is TaskCanceledException && !ct.IsCancellationRequested);
    }

    private Error Unavailable(Exception? exception = null)
    {
        if (exception is not null)
        {
            logger.LogError(exception, "Keycloak's Admin API could not be reached or answered unreadably");
        }

        return Error.Failure(UnavailableErrorCode, "The account service is unavailable.");
    }
}
