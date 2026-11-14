using System.Collections;
using Cvitlo.Resumes.Application.Features.Links.UpdateLink;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Links.UpdateLink.Data;

public class InvalidUpdateLinkData : IEnumerable<TheoryDataRow<UpdateLinkRequest, string>>
{
    public IEnumerator<TheoryDataRow<UpdateLinkRequest, string>> GetEnumerator()
    {
        var source = new CreateLinkRequestFaker(Guid.Empty).Generate();
        var baseRequest = new UpdateLinkRequest(Guid.NewGuid(), Guid.Empty, source.Label, source.Uri);

        yield return new TheoryDataRow<UpdateLinkRequest, string>(
            baseRequest with { Uri = "not-a-valid-url" },
            "uri");

        yield return new TheoryDataRow<UpdateLinkRequest, string>(
            baseRequest with { Uri = "javascript:alert(1)" },
            "uri");

        yield return new TheoryDataRow<UpdateLinkRequest, string>(
            baseRequest with { Uri = "ftp://a.example" },
            "uri");

        yield return new TheoryDataRow<UpdateLinkRequest, string>(
            baseRequest with { Uri = "/relative" },
            "uri");

        yield return new TheoryDataRow<UpdateLinkRequest, string>(
            baseRequest with { Uri = "https://a.example/" + new string('a', 2066) },
            "uri");

        yield return new TheoryDataRow<UpdateLinkRequest, string>(
            baseRequest with { Label = new string('a', 101) },
            "label");
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
