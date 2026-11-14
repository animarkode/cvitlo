using Cvitlo.Common.RateLimiting;
using Cvitlo.Resumes.Application.Groups;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Cvitlo.Resumes.Application.Features.Links.GetAllLinks;

public class GetAllLinksEndpoint
    : Endpoint<GetAllLinksRequest, Results<Ok<GetAllLinksResponse>, NotFound>>
{
    public override void Configure()
    {
        Get("");
        Group<LinksSubGroup>();
        Policies("ResumeOwnerPolicy");
        Version(1);
        Options(x => x.RequireRateLimiting(RateLimitPolicies.AuthenticatedDefault));
    }

    public override async Task<Results<Ok<GetAllLinksResponse>, NotFound>> ExecuteAsync(
        GetAllLinksRequest req,
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