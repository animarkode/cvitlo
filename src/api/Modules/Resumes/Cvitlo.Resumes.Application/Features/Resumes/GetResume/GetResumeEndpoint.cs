using Cvitlo.Auth.PublicApi;
using Cvitlo.Common.RateLimiting;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Application.Groups;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Cvitlo.Resumes.Application.Features.Resumes.GetResume;

public class GetResumeEndpoint
    : Endpoint<GetResumeRequest, Results<Ok<ResumeResponse>, NotFound>>
{
    public override void Configure()
    {
        Get("{Id}");
        Group<ResumesGroup>();
        Version(1);
        Policies("ResumeOwnerPolicy");
        Options(x => x.RequireRateLimiting(RateLimitPolicies.AuthenticatedDefault));
    }

    public override async Task<Results<Ok<ResumeResponse>, NotFound>> ExecuteAsync(
        GetResumeRequest req,
        CancellationToken ct)
    {
        var userId = Guid.Parse(HttpContext.User.FindFirst(JwtClaimTypes.Aliases.UserId)!.Value);
        var result = await req.MapToCommand(userId).ExecuteAsync(ct);

        if (result.IsError)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(result.Value);
    }
}
