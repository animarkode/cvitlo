using Cvitlo.Auth.PublicApi;
using Cvitlo.Common.RateLimiting;
using Cvitlo.Resumes.Application.Groups;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Cvitlo.Resumes.Application.Features.Resumes.UpdateHobbies;

public class UpdateHobbiesEndpoint
    : Endpoint<UpdateHobbiesRequest, Results<NoContent, NotFound>>
{
    public override void Configure()
    {
        Put("{ResumeId}/hobbies");
        Group<ResumesGroup>();
        Version(1);
        Policies("ResumeOwnerPolicy");
        Options(x => x.RequireRateLimiting(RateLimitPolicies.AuthenticatedDefault));
    }

    public override async Task<Results<NoContent, NotFound>> ExecuteAsync(
        UpdateHobbiesRequest req,
        CancellationToken ct)
    {
        var userId = Guid.Parse(HttpContext.User.FindFirst(JwtClaimTypes.Aliases.UserId)!.Value);
        var result = await req.MapToCommand(userId).ExecuteAsync(ct);

        if (result.IsError)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }
}
