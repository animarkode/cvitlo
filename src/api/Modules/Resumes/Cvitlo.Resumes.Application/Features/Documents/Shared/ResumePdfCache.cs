using ErrorOr;
using Cvitlo.Documents.PublicApi;
using Cvitlo.Documents.PublicApi.Resumes;
using Cvitlo.Resumes.Application.Caching;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cvitlo.Resumes.Application.Features.Documents.Shared;

public class ResumePdfCache(
    IServiceScopeFactory scopeFactory,
    HybridCache cache,
    IDocumentsModuleApi documentsModuleApi,
    ILogger<ResumePdfCache> logger)
{
    public async Task<ErrorOr<ResumePdf>> GetOrRenderAsync(Guid userId, Guid resumeId, CancellationToken ct)
    {
        var pdf = await cache.GetOrCreateAsync(
            ResumesCacheKeys.Pdf(userId, resumeId, ResumeTemplate.First, documentsModuleApi.TemplateVersion(ResumeTemplate.First)),
            async cacheCt =>
            {
                // HybridCache shares this factory across concurrent callers, so it owns its scope and calls
                // DocumentCommandHandler directly: the command bus resolves from the first caller's request.
                await using var scope = scopeFactory.CreateAsyncScope();
                var handler = ActivatorUtilities.CreateInstance<DocumentCommandHandler>(scope.ServiceProvider);
                var result = await handler.ExecuteAsync(new DocumentCommand(resumeId, userId), cacheCt);

                if (result.IsError)
                {
                    return null;
                }

                var resume = result.Value;
                var stream = await scope.ServiceProvider.GetRequiredService<IDocumentsModuleApi>().RenderResumeToPdfAsync(
                    resume.ToDocument(), ResumeTemplate.First, cacheCt);

                await using var _ = stream;
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer, cacheCt);

                logger.LogInformation("Rendered the PDF of resume {ResumeId}", resumeId);

                return new ResumePdf(buffer.ToArray(), $"{resume.Person?.JobTitle ?? "resume"}.pdf");
            },
            tags: [ResumesCacheKeys.ResumeTag(resumeId)],
            cancellationToken: ct);

        if (pdf is null)
        {
            return Error.NotFound("Resume.NotFound", $"Resume with id '{resumeId}' was not found");
        }

        return pdf;
    }
}
