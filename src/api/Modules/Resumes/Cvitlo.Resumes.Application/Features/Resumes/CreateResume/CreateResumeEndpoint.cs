using Cvitlo.Auth.PublicApi;
using Cvitlo.Common.RateLimiting;
using Cvitlo.Resumes.Application.Groups;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Cvitlo.Resumes.Application.Features.Resumes.CreateResume;

public class CreateResumeEndpoint
    : Endpoint<CreateResumeRequest, Results<Ok<CreateResumeResponse>, UnauthorizedHttpResult>>
{
    public override void Configure()
    {
        Post("");
        Group<ResumesGroup>();
        Policies("MaxResumeCreationPolicy");
        Version(1);
        Options(x => x.RequireRateLimiting(RateLimitPolicies.AuthenticatedDefault));
    }

    public override async Task<Results<Ok<CreateResumeResponse>, UnauthorizedHttpResult>> ExecuteAsync(
        CreateResumeRequest req,
        CancellationToken ct)
    {
        var userId = HttpContext.User.FindFirst(JwtClaimTypes.Aliases.UserId)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await req.MapToCommand(Guid.Parse(userId)).ExecuteAsync(ct);

        if (result.IsError)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(result.Value);
    }
}
