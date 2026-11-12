using System.Collections;
using Cvitlo.Resumes.Application.Features.Links.CreateLink;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Links.CreateLink.Data;

public class InvalidLinkData : IEnumerable<TheoryDataRow<CreateLinkRequest>>
{
    public IEnumerator<TheoryDataRow<CreateLinkRequest>> GetEnumerator()
    {
        var baseFaker = new CreateLinkRequestFaker(Guid.NewGuid());

        yield return new TheoryDataRow<CreateLinkRequest>(
            baseFaker.RuleFor(r => r.Uri, "not-a-valid-url").Generate());

        yield return new TheoryDataRow<CreateLinkRequest>(
            baseFaker.RuleFor(r => r.Label, new string('a', 256)).Generate());

        yield return new TheoryDataRow<CreateLinkRequest>(
            baseFaker.RuleFor(r => r.Uri, "https://example.com/" + new string('a', 2065)).Generate());
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
