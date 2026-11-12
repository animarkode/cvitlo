using System.Collections;
using Cvitlo.Resumes.Application.Features.Links.UpdateLink;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using static Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.ResumesTestConstants;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Links.UpdateLink.Data;

public class InvalidUpdateLinkData : IEnumerable<TheoryDataRow<UpdateLinkRequest>>
{
    public IEnumerator<TheoryDataRow<UpdateLinkRequest>> GetEnumerator()
    {
        var baseRequest = new CreateLinkRequestFaker(Guid.Empty, LinkSeed).Generate();

        yield return new TheoryDataRow<UpdateLinkRequest>(
            new UpdateLinkRequest(Guid.NewGuid(), Guid.Empty, new string('a', 256), baseRequest.Uri));

        yield return new TheoryDataRow<UpdateLinkRequest>(
            new UpdateLinkRequest(Guid.NewGuid(), Guid.Empty, baseRequest.Label, "not-a-valid-url"));

        yield return new TheoryDataRow<UpdateLinkRequest>(
            new UpdateLinkRequest(Guid.NewGuid(), Guid.Empty, baseRequest.Label,
                "https://example.com/" + new string('a', 2065)));
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
