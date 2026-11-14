using System.Collections;
using Cvitlo.Resumes.Application.Features.Languages.CreateLanguage;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Languages.CreateLanguage.Data;

public class InvalidLanguageData : IEnumerable<TheoryDataRow<CreateLanguageRequest, string>>
{
    public IEnumerator<TheoryDataRow<CreateLanguageRequest, string>> GetEnumerator()
    {
        var baseRequest = new CreateLanguageRequestFaker(Guid.NewGuid()).Generate();

        yield return new TheoryDataRow<CreateLanguageRequest, string>(
            baseRequest with { Name = new string('a', 101) },
            "name");

        yield return new TheoryDataRow<CreateLanguageRequest, string>(
            baseRequest with { Level = "X9" },
            "level");

        yield return new TheoryDataRow<CreateLanguageRequest, string>(
            baseRequest with { Level = "b2" },
            "level");
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
