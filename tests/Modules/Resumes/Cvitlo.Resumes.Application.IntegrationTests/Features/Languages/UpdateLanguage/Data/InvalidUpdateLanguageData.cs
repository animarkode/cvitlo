using System.Collections;
using Cvitlo.Resumes.Application.Features.Languages.UpdateLanguage;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Languages.UpdateLanguage.Data;

public class InvalidUpdateLanguageData : IEnumerable<TheoryDataRow<UpdateLanguageRequest, string>>
{
    public IEnumerator<TheoryDataRow<UpdateLanguageRequest, string>> GetEnumerator()
    {
        var source = new CreateLanguageRequestFaker(Guid.Empty).Generate();
        var baseRequest = new UpdateLanguageRequest(Guid.NewGuid(), Guid.Empty, source.Name, source.Level);

        yield return new TheoryDataRow<UpdateLanguageRequest, string>(
            baseRequest with { Name = new string('a', 101) },
            "name");

        yield return new TheoryDataRow<UpdateLanguageRequest, string>(
            baseRequest with { Level = "X9" },
            "level");

        yield return new TheoryDataRow<UpdateLanguageRequest, string>(
            baseRequest with { Level = "b2" },
            "level");
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
