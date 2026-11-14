using Cvitlo.Common.RateLimiting;
using Cvitlo.Resumes.Application.Groups;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Cvitlo.Resumes.Application.Features.Languages.UpdateLanguage;

public class UpdateLanguageEndpoint
    : Endpoint<UpdateLanguageRequest, Results<NoContent, NotFound>>
{
    public override void Configure()
    {
        Put("{Id}");
        Group<LanguagesSubGroup>();
        Version(1);
        Policies("ResumeOwnerPolicy");
        Options(x => x.RequireRateLimiting(RateLimitPolicies.AuthenticatedDefault));
    }

    public override async Task<Results<NoContent, NotFound>> ExecuteAsync(
        UpdateLanguageRequest req,
        CancellationToken ct)
    {
        var result = await req.MapToCommand().ExecuteAsync(ct);

        if (result.IsError)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }
}
