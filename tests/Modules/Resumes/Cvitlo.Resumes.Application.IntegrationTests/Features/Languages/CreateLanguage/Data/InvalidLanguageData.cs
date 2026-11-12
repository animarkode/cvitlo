using System.Collections;
using Cvitlo.Resumes.Application.Features.Languages.CreateLanguage;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using static Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.ResumesTestConstants;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Languages.CreateLanguage.Data;

public class InvalidLanguageData : IEnumerable<TheoryDataRow<CreateLanguageRequest>>
{
    public IEnumerator<TheoryDataRow<CreateLanguageRequest>> GetEnumerator()
    {
        var resumeId = Guid.Empty;

        yield return new TheoryDataRow<CreateLanguageRequest>(
            new CreateLanguageRequest(resumeId, "Klingon", "InvalidLevel"));

        yield return new TheoryDataRow<CreateLanguageRequest>(
            new CreateLanguageRequest(resumeId, "Korean", "X9"));

        yield return new TheoryDataRow<CreateLanguageRequest>(
            new CreateLanguageRequest(resumeId, new string('a', 101), "B2"));
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
