using System.Collections;
using Cvitlo.Resumes.Application.Features.Projects.UpdateProject;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Projects.UpdateProject.Data;

public class InvalidUpdateProjectData : IEnumerable<TheoryDataRow<UpdateProjectRequest, string>>
{
    public IEnumerator<TheoryDataRow<UpdateProjectRequest, string>> GetEnumerator()
    {
        var source = new CreateProjectRequestFaker(Guid.Empty).Generate();
        var baseRequest = new UpdateProjectRequest(
            Guid.NewGuid(),
            Guid.Empty,
            source.Tagline,
            source.Name,
            source.Url,
            source.StartDate,
            source.EndDate,
            source.Description);

        yield return new TheoryDataRow<UpdateProjectRequest, string>(
            baseRequest with { Name = new string('a', 101) },
            "name");

        yield return new TheoryDataRow<UpdateProjectRequest, string>(
            baseRequest with { Tagline = new string('b', 101) },
            "tagline");

        yield return new TheoryDataRow<UpdateProjectRequest, string>(
            baseRequest with { Url = "javascript:alert(1)" },
            "url");

        yield return new TheoryDataRow<UpdateProjectRequest, string>(
            baseRequest with { StartDate = new DateOnly(2023, 6, 1), EndDate = new DateOnly(2023, 1, 1) },
            "endDate");

        yield return new TheoryDataRow<UpdateProjectRequest, string>(
            baseRequest with { Description = "<p>" + new string('a', 1001) + "</p>" },
            "description");
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
