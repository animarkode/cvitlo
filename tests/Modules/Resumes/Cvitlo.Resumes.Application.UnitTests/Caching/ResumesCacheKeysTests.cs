using Cvitlo.Documents.PublicApi.Resumes;
using Cvitlo.Resumes.Application.Caching;
using Shouldly;

namespace Cvitlo.Resumes.Application.UnitTests.Caching;

public class ResumesCacheKeysTests
{
    [Fact]
    public void Pdf_WithTemplate_ShouldKebabCaseTemplateSegmentAndAppendVersion()
    {
        var userId = Guid.NewGuid();
        var resumeId = Guid.NewGuid();

        var key = ResumesCacheKeys.Pdf(userId, resumeId, ResumeTemplate.First, "v1");

        key.ShouldEndWith($":user:{userId}:pdf:{resumeId}:first:v1");
    }

    [Fact]
    public void Pdf_WithDifferentTemplateVersion_ShouldProduceDifferentKey()
    {
        var userId = Guid.NewGuid();
        var resumeId = Guid.NewGuid();

        var first = ResumesCacheKeys.Pdf(userId, resumeId, ResumeTemplate.First, "v1");
        var second = ResumesCacheKeys.Pdf(userId, resumeId, ResumeTemplate.First, "v2");

        string.Equals(first, second, StringComparison.Ordinal).ShouldBeFalse();
    }
}
