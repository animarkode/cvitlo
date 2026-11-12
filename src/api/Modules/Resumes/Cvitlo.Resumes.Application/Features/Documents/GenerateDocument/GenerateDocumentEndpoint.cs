using Cvitlo.Auth.PublicApi;
using Cvitlo.Common.RateLimiting;
using Cvitlo.Resumes.Application.Features.Documents.Shared;
using Cvitlo.Resumes.Application.Groups;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

namespace Cvitlo.Resumes.Application.Features.Documents.GenerateDocument;

public class GenerateDocumentEndpoint(
    ResumePdfCache resumePdfCache,
    ILogger<GenerateDocumentEndpoint> logger)
    : Endpoint<DocumentRequest, Results<Ok<GenerateDocumentResponse>, NotFound>>
{
    public override void Configure()
    {
        Post("{ResumeId}/generate");
        Group<ResumesGroup>();
        Policies("ResumeOwnerPolicy");
        Version(1);
        Options(x => x.RequireRateLimiting(RateLimitPolicies.ExpensiveResource));
    }

    public override async Task<Results<Ok<GenerateDocumentResponse>, NotFound>> ExecuteAsync(
        DocumentRequest req,
        CancellationToken ct)
    {
        var path = HttpContext.Request.Path.Value;
        logger.LogInformation("[HTTP POST] {Path} started", path);

        var userId = Guid.Parse(HttpContext.User.FindFirst(JwtClaimTypes.Aliases.UserId)!.Value);
        var result = await resumePdfCache.GetOrRenderAsync(userId, req.ResumeId, ct);

        if (result.IsError)
        {
            logger.LogWarning("[HTTP POST] {Path} failed: {Error}", path, result.FirstError.Description);

            return TypedResults.NotFound();
        }

        var pdf = result.Value;
        var base64String = Convert.ToBase64String(pdf.Content);

        logger.LogInformation("[HTTP POST] {Path} completed successfully", path);

        return TypedResults.Ok(new GenerateDocumentResponse(base64String));
    }
}
