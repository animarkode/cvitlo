using System.Collections;
using Cvitlo.Resumes.Application.Features.Projects.CreateProject;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Projects.CreateProject.Data;

public class InvalidProjectData : IEnumerable<TheoryDataRow<CreateProjectRequest>>
{
    public IEnumerator<TheoryDataRow<CreateProjectRequest>> GetEnumerator()
    {
        var baseRequest = new CreateProjectRequestFaker(Guid.NewGuid()).Generate();

        yield return new TheoryDataRow<CreateProjectRequest>(
            baseRequest with { StartDate = new DateOnly(2024, 12, 31), EndDate = new DateOnly(2024, 1, 1) });

        yield return new TheoryDataRow<CreateProjectRequest>(
            baseRequest with { Description = new string('A', 1001) });

        yield return new TheoryDataRow<CreateProjectRequest>(
            baseRequest with { StartDate = new DateOnly(2099, 1, 1) });

        yield return new TheoryDataRow<CreateProjectRequest>(
            baseRequest with { Name = new string('a', 101) });

        yield return new TheoryDataRow<CreateProjectRequest>(
            baseRequest with { Tagline = new string('b', 101) });
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}