using System.Collections;
using Cvitlo.Resumes.Application.Features.Projects.CreateProject;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Projects.CreateProject.Data;

public class InvalidProjectData : IEnumerable<TheoryDataRow<CreateProjectRequest, string>>
{
    public IEnumerator<TheoryDataRow<CreateProjectRequest, string>> GetEnumerator()
    {
        var baseRequest = new CreateProjectRequestFaker(Guid.NewGuid()).Generate();

        yield return new TheoryDataRow<CreateProjectRequest, string>(
            baseRequest with { Name = new string('a', 101) },
            "name");

        yield return new TheoryDataRow<CreateProjectRequest, string>(
            baseRequest with { Tagline = new string('b', 101) },
            "tagline");

        yield return new TheoryDataRow<CreateProjectRequest, string>(
            baseRequest with { Url = "javascript:alert(1)" },
            "url");

        yield return new TheoryDataRow<CreateProjectRequest, string>(
            baseRequest with { StartDate = new DateOnly(2024, 12, 31), EndDate = new DateOnly(2024, 1, 1) },
            "endDate");

        yield return new TheoryDataRow<CreateProjectRequest, string>(
            baseRequest with { StartDate = new DateOnly(2099, 1, 1), EndDate = null },
            "startDate");

        yield return new TheoryDataRow<CreateProjectRequest, string>(
            baseRequest with { Description = "<p>" + new string('a', 1001) + "</p>" },
            "description");
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
