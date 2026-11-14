using Cvitlo.Common.RateLimiting;
using Cvitlo.Resumes.Application.Groups;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Cvitlo.Resumes.Application.Features.Skills.GetAllSkills;

public class GetAllSkillsEndpoint
    : Endpoint<GetAllSkillsRequest, Results<Ok<GetAllSkillsResponse>, NotFound>>
{
    public override void Configure()
    {
        Get("");
        Group<SkillsSubGroup>();
        Version(1);
        Policies("ResumeOwnerPolicy");
        Options(x => x.RequireRateLimiting(RateLimitPolicies.AuthenticatedDefault));
    }

    public override async Task<Results<Ok<GetAllSkillsResponse>, NotFound>> ExecuteAsync(
        GetAllSkillsRequest req,
        CancellationToken ct)
    {
        var result = await req.MapToCommand().ExecuteAsync(ct);

        if (result.IsError)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(result.Value);
    }
}
