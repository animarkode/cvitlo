using System.Security.Claims;
using ErrorOr;
using Cvitlo.Auth.Application.Authentication;
using Cvitlo.Auth.PublicApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Cvitlo.Auth.Application.Features.Account;

public static class AccountAccess
{
    public static async Task<ErrorOr<string>> GetAccessTokenAsync(this HttpContext context)
    {
        var token = await context.GetTokenAsync(AuthSchemes.Session, "access_token");

        return string.IsNullOrEmpty(token)
            ? Error.Unauthorized("Account.Unauthorized", "There is no signed-in session.")
            : token;
    }

    public static ErrorOr<string> GetSubject(this ClaimsPrincipal user)
    {
        var subject = user.FindFirst(JwtClaimTypes.Registered.Sub)?.Value;

        return string.IsNullOrEmpty(subject)
            ? Error.Unauthorized("Account.Unauthorized", "There is no signed-in session.")
            : subject;
    }

    // Keycloak's field errors become a validation problem, so the form can show each beside its field.
    public static ProblemHttpResult ToProblem(this Error error)
    {
        return error.Type switch
        {
            ErrorType.Validation => ToValidationProblem(error),
            ErrorType.Unauthorized => ToProblem(error, StatusCodes.Status401Unauthorized),
            ErrorType.Forbidden => ToProblem(error, StatusCodes.Status403Forbidden),
            ErrorType.Conflict => ToProblem(error, StatusCodes.Status409Conflict),
            ErrorType.NotFound => ToProblem(error, StatusCodes.Status404NotFound),
            _ => ToProblem(error, StatusCodes.Status502BadGateway)
        };
    }

    private static ProblemHttpResult ToProblem(Error error, int statusCode)
    {
        return TypedResults.Problem(
            error.Description,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?>(StringComparer.Ordinal) { ["code"] = error.Code });
    }

    private static ProblemHttpResult ToValidationProblem(Error error)
    {
        if (error.Metadata is not { Count: > 0 } fields)
        {
            return TypedResults.Problem(error.Description, statusCode: StatusCodes.Status400BadRequest);
        }

        var errors = fields.ToDictionary(
            field => field.Key,
            field => new[] { field.Value?.ToString() ?? error.Description },
            StringComparer.Ordinal);

        return TypedResults.Problem(new HttpValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Detail = error.Description
        });
    }
}
