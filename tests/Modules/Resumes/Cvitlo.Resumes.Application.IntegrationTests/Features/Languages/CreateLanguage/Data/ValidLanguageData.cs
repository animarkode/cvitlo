using System.Collections;
using Cvitlo.Resumes.Application.Features.Languages.CreateLanguage;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using static Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.ResumesTestConstants;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Languages.CreateLanguage.Data;

public class ValidLanguageData : IEnumerable<TheoryDataRow<CreateLanguageRequest>>
{
    private const int Count = 3;

    public IEnumerator<TheoryDataRow<CreateLanguageRequest>> GetEnumerator()
    {
        foreach (var r in new CreateLanguageRequestFaker(Guid.Empty, LanguageSeed).Generate(Count))
        {
            yield return new TheoryDataRow<CreateLanguageRequest>(r);
        }

        yield return new TheoryDataRow<CreateLanguageRequest>(
            new CreateLanguageRequest(Guid.Empty, null, null));

        yield return new TheoryDataRow<CreateLanguageRequest>(
            new CreateLanguageRequest(Guid.Empty, string.Empty, string.Empty));
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
