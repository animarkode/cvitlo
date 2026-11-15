using Cvitlo.Documents.PublicApi.Resumes;

namespace Cvitlo.Resumes.Application.Caching;

// Scheme and invalidation: docs/decisions/caching.md.
public static class ResumesCacheKeys
{
    // Embedded in every key so a new build busts stale entries instead of deserializing old shapes.
    private static readonly Guid Build = typeof(ResumesCacheKeys).Assembly.ManifestModule.ModuleVersionId;

    public static string List(Guid userId)
    {
        return $"resumes:{Build}:user:{userId}:list";
    }

    public static string Resume(Guid userId, Guid resumeId)
    {
        return $"resumes:{Build}:user:{userId}:resume:{resumeId}";
    }

    public static string Pdf(Guid userId, Guid resumeId, ResumeTemplate template, string templateVersion)
    {
        return $"resumes:{Build}:user:{userId}:pdf:{resumeId}:{TemplateSegment(template)}:{templateVersion}";
    }

    // Spelled out instead of derived from the enum name, so renaming a member does not move its cache keys.
    private static string TemplateSegment(ResumeTemplate template) => template switch
    {
        ResumeTemplate.First => "first",
        _ => throw new ArgumentOutOfRangeException(nameof(template), template, null)
    };

    public static string ResumeTag(Guid resumeId)
    {
        return $"resume:{resumeId}";
    }

    public static string UserResumesTag(Guid userId)
    {
        return $"user:{userId}:resumes";
    }
}
