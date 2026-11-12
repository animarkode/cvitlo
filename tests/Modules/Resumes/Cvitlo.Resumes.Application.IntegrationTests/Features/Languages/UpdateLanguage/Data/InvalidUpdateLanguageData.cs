using System.Collections;
using Cvitlo.Resumes.Application.Features.Languages.UpdateLanguage;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Languages.UpdateLanguage.Data;

public class InvalidUpdateLanguageData : IEnumerable<TheoryDataRow<UpdateLanguageRequest>>
{
    public IEnumerator<TheoryDataRow<UpdateLanguageRequest>> GetEnumerator()
    {
        var baseFaker = new CreateLanguageRequestFaker(Guid.Empty);

        yield return new TheoryDataRow<UpdateLanguageRequest>(
            new UpdateLanguageRequest(Guid.Empty, Guid.Empty, baseFaker.Generate().Name,
                "InvalidLevel"));

        yield return new TheoryDataRow<UpdateLanguageRequest>(
            new UpdateLanguageRequest(Guid.Empty, Guid.Empty, baseFaker.Generate().Name,
                "X9"));

        yield return new TheoryDataRow<UpdateLanguageRequest>(
            new UpdateLanguageRequest(Guid.Empty, Guid.Empty, new string('a', 101),
                baseFaker.Generate().Level));
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
