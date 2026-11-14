using Cvitlo.Auth.PublicApi;
using Cvitlo.Common.RateLimiting;
using Cvitlo.Resumes.Application.Features.Documents.Shared;
using Cvitlo.Resumes.Application.Groups;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Cvitlo.Resumes.Application.Features.Documents.DownloadDocument;

public class DownloadDocumentEndpoint(ResumePdfCache resumePdfCache)
    : Endpoint<DocumentRequest, Results<FileContentHttpResult, NotFound>>
{
    public override void Configure()
    {
        Get("{ResumeId}/download");
        Group<ResumesGroup>();
        Policies("ResumeOwnerPolicy");
        Version(1);
        Options(x => x.RequireRateLimiting(RateLimitPolicies.ExpensiveResource));
    }

    public override async Task<Results<FileContentHttpResult, NotFound>> ExecuteAsync(
        DocumentRequest req,
        CancellationToken ct)
    {
        var userId = Guid.Parse(HttpContext.User.FindFirst(JwtClaimTypes.Aliases.UserId)!.Value);
        var result = await resumePdfCache.GetOrRenderAsync(userId, req.ResumeId, ct);

        if (result.IsError)
        {
            return TypedResults.NotFound();
        }

        var pdf = result.Value;

        return TypedResults.File(pdf.Content, "application/pdf", pdf.FileName);
    }
}
