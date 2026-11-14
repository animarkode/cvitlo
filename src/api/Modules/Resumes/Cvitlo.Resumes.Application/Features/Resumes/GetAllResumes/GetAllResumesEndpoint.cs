using Cvitlo.Auth.PublicApi;
using Cvitlo.Common.RateLimiting;
using Cvitlo.Resumes.Application.Groups;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Cvitlo.Resumes.Application.Features.Resumes.GetAllResumes;

public class GetAllResumesEndpoint
    : EndpointWithoutRequest<Results<Ok<GetAllResumesResponse>, UnauthorizedHttpResult>>
{
    public override void Configure()
    {
        Get("");
        Group<ResumesGroup>();
        Version(1);
        Options(x => x.RequireRateLimiting(RateLimitPolicies.AuthenticatedDefault));
    }

    public override async Task<Results<Ok<GetAllResumesResponse>, UnauthorizedHttpResult>> ExecuteAsync(
        CancellationToken ct)
    {
        var userId = HttpContext.User.FindFirst(JwtClaimTypes.Aliases.UserId)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await new GetAllResumesCommand(Guid.Parse(userId)).ExecuteAsync(ct);

        return TypedResults.Ok(result.Value);
    }
}
