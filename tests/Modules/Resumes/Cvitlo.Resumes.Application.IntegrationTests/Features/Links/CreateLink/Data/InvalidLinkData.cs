using System.Collections;
using Cvitlo.Resumes.Application.Features.Links.CreateLink;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Links.CreateLink.Data;

public class InvalidLinkData : IEnumerable<TheoryDataRow<CreateLinkRequest, string>>
{
    public IEnumerator<TheoryDataRow<CreateLinkRequest, string>> GetEnumerator()
    {
        var baseRequest = new CreateLinkRequestFaker(Guid.NewGuid()).Generate();

        yield return new TheoryDataRow<CreateLinkRequest, string>(
            baseRequest with { Uri = "not-a-valid-url" },
            "uri");

        yield return new TheoryDataRow<CreateLinkRequest, string>(
            baseRequest with { Uri = "javascript:alert(1)" },
            "uri");

        yield return new TheoryDataRow<CreateLinkRequest, string>(
            baseRequest with { Uri = "ftp://a.example" },
            "uri");

        yield return new TheoryDataRow<CreateLinkRequest, string>(
            baseRequest with { Uri = "/relative" },
            "uri");

        yield return new TheoryDataRow<CreateLinkRequest, string>(
            baseRequest with { Uri = "https://a.example/" + new string('a', 2066) },
            "uri");

        yield return new TheoryDataRow<CreateLinkRequest, string>(
            baseRequest with { Label = new string('a', 101) },
            "label");
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
