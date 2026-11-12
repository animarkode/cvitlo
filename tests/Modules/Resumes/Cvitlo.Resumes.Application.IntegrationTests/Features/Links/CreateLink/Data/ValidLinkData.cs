using System.Collections;
using Cvitlo.Resumes.Application.Features.Links.CreateLink;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using static Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.ResumesTestConstants;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Links.CreateLink.Data;

public class ValidLinkData : IEnumerable<TheoryDataRow<CreateLinkRequest>>
{
    private const int Count = 3;

    public IEnumerator<TheoryDataRow<CreateLinkRequest>> GetEnumerator()
    {
        foreach (var r in new CreateLinkRequestFaker(Guid.Empty, LinkSeed).Generate(Count))
        {
            yield return new TheoryDataRow<CreateLinkRequest>(r);
        }

        yield return new TheoryDataRow<CreateLinkRequest>(
            new CreateLinkRequest(Guid.Empty, null, null));

        yield return new TheoryDataRow<CreateLinkRequest>(
            new CreateLinkRequest(Guid.Empty, string.Empty, string.Empty));
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
